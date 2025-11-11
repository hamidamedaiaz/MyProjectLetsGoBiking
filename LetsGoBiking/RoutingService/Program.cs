using System;
using System.Web.Http;
using System.Web.Http.SelfHost;
using System.Web.Http.Cors;

namespace RoutingService
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║   Démarrage du RoutingService          ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.WriteLine();

            try
            {

                var config = new HttpSelfHostConfiguration("http://localhost:8080");

                // Activer CORS pour permettre les appels depuis le navigateur
                var cors = new EnableCorsAttribute("*", "*", "*");
                config.EnableCors(cors);

                // Configuration des routes
                config.Routes.MapHttpRoute(
                    name: "DefaultApi",
                    routeTemplate: "{controller}/{action}",
                    defaults: null
                );

                // Formatter JSON par défaut
                config.Formatters.JsonFormatter.SerializerSettings.Formatting =
                    Newtonsoft.Json.Formatting.Indented;

                using (var server = new HttpSelfHostServer(config))
                {
                    server.OpenAsync().Wait();

                    Console.WriteLine("✅ RoutingService démarré avec succès !");
                    Console.WriteLine();
                    Console.WriteLine("📍 URL de base    : http://localhost:8080");
                    Console.WriteLine("📍 Endpoint       : http://localhost:8080/itinerary/compute");
                    Console.WriteLine();
                    Console.WriteLine("🔧 Configuration  :");
                    Console.WriteLine("   - CORS activé");
                    Console.WriteLine("   - Format JSON");
                    Console.WriteLine("   - Appels directs : OpenRoute, OpenStreet");
                    Console.WriteLine("   - Appel SOAP     : ProxyCache (JCDecaux)");
                    Console.WriteLine();
                    Console.WriteLine("─────────────────────────────────────────");
                    Console.WriteLine("Appuyez sur ENTRÉE pour arrêter le serveur...");
                    Console.WriteLine("─────────────────────────────────────────");

                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("❌ ERREUR lors du démarrage du serveur :");
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("Détails : " + ex.InnerException.Message);
                }
                Console.WriteLine();
                Console.WriteLine("Appuyez sur ENTRÉE pour quitter...");
                Console.ReadLine();
            }
        }
    }
}