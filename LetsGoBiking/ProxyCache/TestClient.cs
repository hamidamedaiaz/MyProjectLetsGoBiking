using System;
using System.ServiceModel;
using System.Threading.Tasks;
using ProxyCache.Services;

namespace ProxyCache
{
    /// <summary>
    /// Client de test pour vérifier le fonctionnement du ProxyCache
    /// </summary>
    public class TestClient
    {
        public static async Task RunTests()
        {
            Console.WriteLine();
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║   Test du ProxyCache Service           ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.WriteLine();

            var binding = new BasicHttpBinding
            {
                MaxReceivedMessageSize = 2147483647,
                ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas
                {
                    MaxDepth = 64,
                    MaxStringContentLength = 2147483647,
                    MaxArrayLength = 2147483647
                }
            };

            var endpoint = new EndpointAddress("http://localhost:8081/ProxyCache");
            var factory = new ChannelFactory<IProxyCache>(binding, endpoint);
            
            try
            {
                var client = factory.CreateChannel();

                // Test 1 : Premier appel (devrait aller chercher les données)
                Console.WriteLine("🧪 Test 1 : Premier appel pour 'Lyon'");
                Console.WriteLine("─────────────────────────────────────────");
                var start1 = DateTime.Now;
                var stations1 = await client.GetStationsAsync("Lyon");
                var duration1 = (DateTime.Now - start1).TotalSeconds;
                
                Console.WriteLine($"✅ Résultat : {stations1.Count} stations récupérées");
                Console.WriteLine($"⏱️  Durée : {duration1:F2} secondes");
                Console.WriteLine();

                if (stations1.Count > 0)
                {
                    Console.WriteLine("📍 Exemple de station :");
                    Console.WriteLine($"   {stations1[0]}");
                    Console.WriteLine();
                }

                // Test 2 : Deuxième appel immédiat (devrait utiliser le cache)
                Console.WriteLine("🧪 Test 2 : Deuxième appel pour 'Lyon' (cache attendu)");
                Console.WriteLine("─────────────────────────────────────────");
                var start2 = DateTime.Now;
                var stations2 = await client.GetStationsAsync("Lyon");
                var duration2 = (DateTime.Now - start2).TotalSeconds;
                
                Console.WriteLine($"✅ Résultat : {stations2.Count} stations récupérées");
                Console.WriteLine($"⏱️  Durée : {duration2:F2} secondes");
                Console.WriteLine();

                // Comparaison des temps
                if (duration2 < duration1 / 2)
                {
                    Console.WriteLine("💾 ✅ CACHE FONCTIONNE ! Le 2ème appel est beaucoup plus rapide.");
                }
                else
                {
                    Console.WriteLine("⚠️  Le cache ne semble pas fonctionner (même durée).");
                }
                Console.WriteLine();

                // Test 3 : Autre ville
                Console.WriteLine("🧪 Test 3 : Appel pour une autre ville 'Paris'");
                Console.WriteLine("─────────────────────────────────────────");
                var start3 = DateTime.Now;
                var stations3 = await client.GetStationsAsync("Paris");
                var duration3 = (DateTime.Now - start3).TotalSeconds;
                
                Console.WriteLine($"✅ Résultat : {stations3.Count} stations récupérées");
                Console.WriteLine($"⏱️  Durée : {duration3:F2} secondes");
                Console.WriteLine();

                Console.WriteLine("═══════════════════════════════════════");
                Console.WriteLine("✅ TOUS LES TESTS TERMINÉS");
                Console.WriteLine("═══════════════════════════════════════");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR : {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Détails : {ex.InnerException.Message}");
                }
            }
            finally
            {
                factory.Close();
            }
        }
    }
}