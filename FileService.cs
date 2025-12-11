using System.Text.Json;
using System.Text.Json.Serialization;
using WinFormsCargoApp.Logic;
using WinFormsCargoApp.Models;

namespace WinFormsCargoApp.Services
{
    public static class FileService
    {
        private static readonly JsonSerializerOptions opts = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static void Save(string path, CargoCompany company)
        {
            var model = new DataModel
            {
                Clients = company.Clients,
                Tariffs = company.Tariffs,
                Orders = company.Orders
            };
            var json = JsonSerializer.Serialize(model, opts);
            File.WriteAllText(path, json);
        }

        public static CargoCompany Load(string path)
        {
            var json = File.ReadAllText(path);
            var model = JsonSerializer.Deserialize<DataModel>(json, opts);
            var company = new CargoCompany();
            if (model != null)
            {
                company.Clients = model.Clients ?? new System.Collections.Generic.List<Client>();
                company.Tariffs = model.Tariffs ?? new System.Collections.Generic.List<Tariff>();
                company.Orders = model.Orders ?? new System.Collections.Generic.List<Order>();
                company.FixNextId();
            }
            return company;
        }
    }
}
