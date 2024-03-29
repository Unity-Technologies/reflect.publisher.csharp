﻿using System;
using System.Collections.Generic;
using System.Numerics;
using Unity.Reflect.Model;
using Unity.Reflect.Utils;
using Unity.Reflect.UI;
using System.Reflection;
using System.Timers;
using System.IO;
using Unity.Reflect;

namespace PublisherSample
{
    public class PublisherSample
    {
        internal const string PublisherLogComponent = "PublisherSample";

        public static void Run(PublishType publishType)
        {
            var sample = new PublisherSample();
            sample.Publish(publishType);
        }

        IPublisherClient m_PublisherClient;
        Random m_Random = new Random();

        void Publish(PublishType publishType)
        {
            try
            {
                // This indicates the name and version of the software used for publishing.
                var pluginName = "C# Publisher Sample";
                var pluginVersion = Assembly.GetExecutingAssembly().GetName().Version;

                // Display the project selection external window and return when the project selection has ended (whether it succeeded or not).
                var settings = WindowFactory.ShowPublisherSettings(pluginName, pluginVersion, publishType);

                // If the user cancels (or in a few other cases), we don't want to export data.
                if (settings != null)
                {
                    Logger.Info($"Logged in as {settings.User.DisplayName}", PublisherLogComponent);
                    Logger.Info($"Target project : {settings.TargetProject.Name}", PublisherLogComponent);
                    Logger.Info($"Target sync server : {settings.TargetProject.Host.ServerName}", PublisherLogComponent);

                    // Let's customize the settings before opening the client
                    CustomizePublisherSettings(settings);

                    // This is the public name of the Source Project you want to export (it doesn't have to be unique).
                    var sourceName = "C# Sample Quad";

                    // This identifies the Source Project you want to export and must be unique and persistent over multiple publishing sessions.
                    var sourceId = "internal guid";

                    // Create a Publisher Client, that will allow us to publish data into the selected Target Project.
                    m_PublisherClient = Publisher.OpenClient(sourceName, sourceId, settings);

                    if (publishType == PublishType.Export)
                    {
                        // Simple export flow.
                        PerformExportTransaction();

                        // Properly close the connection to the SyncServer.
                        m_PublisherClient.CloseAndWait();
                    }
                    else if (publishType == PublishType.ExportAndSync)
                    {
                        // Sync flow (first export + sync updates).
                        PerformExportTransaction();
                        PerformSyncUpdates();
                    }
                }
                else
                {
                    Logger.Info("No project selected.", PublisherLogComponent);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString(), PublisherLogComponent);
            }
        }

        void CustomizePublisherSettings(PublisherSettings settings)
        {
            settings.LengthUnit = LengthUnit.Meters;
            settings.AxisInversion = AxisInversion.None;

            // Provide rules if we can find them in a rules file
            const string rulesFileName = "rules.json";
            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            var assemblyRulesPath = Path.Combine(Path.GetDirectoryName(assemblyPath), rulesFileName);
            if (File.Exists(assemblyRulesPath))
            {
                settings.Rules = File.ReadAllText(assemblyRulesPath);
            }
        }

        void PerformExportTransaction()
        {
            // Report the progress of the export to 0%
            m_PublisherClient.ReportProgress(0);

            // Start a transaction ; note that the publisher client can only run one transaction at a time.
            var transaction = m_PublisherClient.StartTransaction();

            // Build a SyncMesh and send it to the server
            var mesh = BuildMesh();
            transaction.Send(mesh);

            // Build a SyncMaterial and send it to the server
            var material = BuildMaterial(SyncColor.White);
            transaction.Send(material);

            // Report in between parsing progress
            m_PublisherClient.ReportProgress(50);

            // Build a SyncObject and send it to the server.
            var obj = BuildObject(mesh, material);
            transaction.Send(obj);

            // Build a SyncObjectInstance and send it to the server
            var instance = BuildObjectInstance(obj);
            transaction.Send(instance);

            // Commit the transaction, then detach it from the publisher client.
            transaction.Commit();

            // Report the completion of the export
            m_PublisherClient.ReportProgress(100);
        }

        void PerformSyncUpdates()
        {
            // Usually you would subscribe to a callback in the software API to detect changes in the scene.
            // In this sample we'll emulate that with a timer.
            var timer = new Timer();
            timer.Interval = 3000;
            timer.AutoReset = true;
            timer.Elapsed += OnSyncUpdate;
            timer.Disposed += OnSyncStop;
            timer.Start();
        }

        void OnSyncUpdate(object source, ElapsedEventArgs e)
        {
            // Let's start a sync update transaction
            var transaction = m_PublisherClient.StartTransaction();

            // Next step is to send only what has changed
            // Here we only override the material with a new random color
            var randomColor = SyncColor.From256(
                m_Random.Next(0, 255),
                m_Random.Next(0, 255),
                m_Random.Next(0, 255)
            );
            var material = BuildMaterial(randomColor);
            transaction.Send(material);

            // Finally, let's commit the transaction
            transaction.Commit();
        }

        void OnSyncStop(object sender, EventArgs e)
        {
            // When the sync stops, we can close the client
            m_PublisherClient.CloseAndWait();
        }

        SyncMesh BuildMesh()
        {
            // Create a hardcoded quad mesh
            return new SyncMesh(new SyncId("Mesh id"), "Quad mesh")
            {
                Vertices = new List<Vector3>
                {
                    new Vector3(0, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(1, 1, 0)
                },

                Normals = new List<Vector3>
                {
                    new Vector3(0, 0, 1),
                    new Vector3(0, 0, 1),
                    new Vector3(0, 0, 1),
                    new Vector3(0, 0, 1)
                },

                Uvs = new List<Vector2>
                {
                    new Vector2(0, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 0),
                    new Vector2(1, 1)
                },

                SubMeshes = new List<SyncSubMesh>
                {
                    new SyncSubMesh
                    {
                        Triangles = new List<int> { 0, 1, 2, 1, 3, 2 }
                    }
                }
            };
        }

        SyncMaterial BuildMaterial(SyncColor color)
        {
            // Create a basic colored material
            var material = new SyncMaterial(new SyncId("Material id"));
            material.AlbedoColor = color;
            return material;
        }

        SyncObject BuildObject(SyncMesh mesh, SyncMaterial material)
        {
            // Create a parent object
            var parentObject = new SyncObject(new SyncId("Parent id"), "Quad Object Parent")
            {
                Transform = SyncTransform.Identity
            };

            // Create a child object with geometry
            var childObject = new SyncObject(new SyncId("Child id"), "Quad Object Child")
            {
                MeshId = mesh.Id,
                MaterialIds = new List<SyncId> { material.Id },
                Transform = SyncTransform.Identity
            };
            parentObject.Children.Add(childObject);

            // Fill the object with metadata
            parentObject.Metadata.Add("Key 1", new SyncParameter("Value", "Group", true));
            parentObject.Metadata.Add("Key 2", new SyncParameter("Other value", "Group", true));

            // Uncomment the following line to add the "Merge" metadata key.
            // Because of the rules.json file, this would trigger the MergeChildObjects action.
            //parentObject.Metadata.Add("Merge", new SyncParameter("", "Rules", true));

            return parentObject;
        }

        SyncObjectInstance BuildObjectInstance(SyncObject obj)
        {
            // Create an instance out of the SyncObject
            return new SyncObjectInstance(new SyncId("Instance id"), "Quad instance", obj.Id)
            {
                Transform = SyncTransform.Identity,
                Metadata = obj.Metadata
            };
        }
    }
}
