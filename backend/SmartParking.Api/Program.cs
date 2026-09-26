using System;
using System.Configuration;
using Microsoft.Owin.Hosting;
using SmartParking.Api.Data;
using SmartParking.Api.Infrastructure;

namespace SmartParking.Api
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var db = DatabaseInitializer.EnsureCreated();
            AppServices.Initialize(db);

            using (new BookingReleaseTimer(AppServices.Bookings))
            {
                var baseUrl = ConfigurationManager.AppSettings["BaseUrl"] ?? "http://localhost:4000/";
                using (WebApp.Start<Startup>(baseUrl))
                {
                    Console.WriteLine("Smart Parking API listening on " + baseUrl);
                    Console.WriteLine("Press Enter to stop.");
                    Console.ReadLine();
                }
            }
        }
    }
}
