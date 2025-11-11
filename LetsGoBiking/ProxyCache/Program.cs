using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using ProxyCache.Services;

namespace ProxyCache
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║   Démarrage du ProxyCache Server       ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.WriteLine();

            try
            {
                using (ServiceHost host = new ServiceHost(typeof(ProxyCacheService)))
                {
                    // Configuration du endpoint SOAP
                    var binding = new BasicHttpBinding
                    {
                        MaxReceivedMessageSize = 2147483647,
                        MaxBufferSize = 2147483647,
                        ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max
                    };

                    host.AddServiceEndpoint(
                        typeof(IProxyCache),
                        binding,
                        "http://localhost:8081/ProxyCache"
                    );

                    // Activation des métadonnées pour générer le client SOAP
                    ServiceMetadataBehavior smb = host.Description.Behaviors.Find<ServiceMetadataBehavior>();
                    if (smb == null)
                    {
                        smb = new ServiceMetadataBehavior();
                        host.Description.Behaviors.Add(smb);
                    }
                    smb.HttpGetEnabled = true;
                    smb.HttpGetUrl = new Uri("http://localhost:8081/ProxyCache/mex");

                    // Démarrage du serveur
                    host.Open();

                    Console.WriteLine("✅ ProxyCache Server démarré avec succès !");
                    Console.WriteLine();
                    Console.WriteLine("📍 Endpoint SOAP : http://localhost:8081/ProxyCache");
                    Console.WriteLine("📍 Métadonnées   : http://localhost:8081/ProxyCache/mex");
                    Console.WriteLine();
                    Console.WriteLine("💾 Cache actif : 5 minutes");
                    Console.WriteLine();
                    Console.WriteLine("─────────────────────────────────────────");
                    Console.WriteLine("Appuyez sur ENTRÉE pour arrêter le serveur...");
                    Console.WriteLine("─────────────────────────────────────────");

                    Console.ReadLine();

                    host.Close();
                    Console.WriteLine();
                    Console.WriteLine("❌ ProxyCache Server arrêté.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("❌ ERREUR lors du démarrage du serveur :");
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                Console.WriteLine("Appuyez sur ENTRÉE pour quitter...");
                Console.ReadLine();
            }
        }
    }
}