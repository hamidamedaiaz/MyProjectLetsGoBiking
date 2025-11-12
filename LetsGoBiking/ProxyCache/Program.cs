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

            ServiceHost host = null;

            try
            {
                // ✅ Utilise uniquement la configuration de App.config
                host = new ServiceHost(typeof(ProxyCacheService));

                // Démarrage du serveur
                Console.WriteLine("🔄 Ouverture du ServiceHost...");
                host.Open();

                Console.WriteLine("✅ ProxyCache Server démarré avec succès !");
                Console.WriteLine();
                Console.WriteLine("📍 Endpoint SOAP : http://localhost:8081/ProxyCache");
                Console.WriteLine("📍 Métadonnées   : http://localhost:8081/ProxyCache/mex");
                Console.WriteLine();
                Console.WriteLine("💾 Cache actif : 5 minutes");
                Console.WriteLine();
                Console.WriteLine("─────────────────────────────────────────");
                Console.WriteLine("Tapez 'test' pour lancer les tests");
                Console.WriteLine("Appuyez sur ENTRÉE pour arrêter le serveur...");
                Console.WriteLine("─────────────────────────────────────────");

                string input = Console.ReadLine();
                
                // ✅ Lancer les tests si demandé
                if (input?.ToLower() == "test")
                {
                    TestClient.RunTests().Wait();
                    Console.WriteLine();
                    Console.WriteLine("Appuyez sur ENTRÉE pour arrêter le serveur...");
                    Console.ReadLine();
                }

                if (host.State == CommunicationState.Opened)
                {
                    host.Close();
                }
                
                Console.WriteLine();
                Console.WriteLine("❌ ProxyCache Server arrêté.");
            }
            catch (AddressAlreadyInUseException)
            {
                Console.WriteLine();
                Console.WriteLine("❌ ERREUR : Le port 8081 est déjà utilisé !");
                Console.WriteLine("   → Vérifiez qu'aucune autre instance n'est en cours d'exécution.");
                Console.WriteLine("   → Vous pouvez utiliser 'netstat -ano | findstr :8081' pour identifier le processus.");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine();
                Console.WriteLine("❌ ERREUR de configuration du service :");
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine();
                    Console.WriteLine("Détails :");
                    Console.WriteLine(ex.InnerException.Message);
                    Console.WriteLine(ex.InnerException.StackTrace);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("❌ ERREUR lors du démarrage du serveur :");
                Console.WriteLine(ex.GetType().Name + ": " + ex.Message);
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine();
                    Console.WriteLine("Détails internes :");
                    Console.WriteLine(ex.InnerException.GetType().Name + ": " + ex.InnerException.Message);
                    
                    if (ex.InnerException.InnerException != null)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Détails supplémentaires :");
                        Console.WriteLine(ex.InnerException.InnerException.Message);
                    }
                }
                
                Console.WriteLine();
                Console.WriteLine("Stack trace :");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                if (host != null)
                {
                    if (host.State == CommunicationState.Faulted)
                    {
                        host.Abort();
                    }
                    else if (host.State == CommunicationState.Opened)
                    {
                        try { host.Close(); }
                        catch { host.Abort(); }
                    }
                }
                
                Console.WriteLine();
                Console.WriteLine("Appuyez sur ENTRÉE pour quitter...");
                Console.ReadLine();
            }
        }
    }
}