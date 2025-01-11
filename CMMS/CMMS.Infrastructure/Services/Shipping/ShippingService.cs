using AutoMapper;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Services.Shipping
{
    public interface IShippingService
    {
        Task<List<StoreDistance>> GetListStoreOrderbyDeliveryDistance(string deliveryAddress, List<Store> stores);
        Task<StoreDistance> GeStoreOrderbyDeliveryDistance(string deliveryAddress, Store stores);
        Task<string> ResponseLatitueLongtitueValue(string deliveryAddress);
        decimal CalculateShippingFee(decimal distance, decimal weight);
        double CalculateDistanceBetweenPostionLatLon(double lat1, double lon1, double lat2, double lon2);
    }
    public class ShippingService : IShippingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IConfigurationShippingServices _configurationShippingServices;

        public ShippingService(HttpClient httpClient,  IMapper mapper, 
            IStoreService storeService, IConfiguration configuration, IConfigurationShippingServices configurationShippingServices)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _configurationShippingServices = configurationShippingServices;
        }

        public double CalculateDistanceBetweenPostionLatLon(double lat1, double lon1, double lat2, double lon2)
        {
            const double EarthRadiusKm = 6371;

            double dLat = DegreesToRadians(lat2 - lat1);
            double dLon = DegreesToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            // return kilometers value
            return EarthRadiusKm * c ;
        }

        private  double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        public decimal CalculateShippingFee(decimal distance, decimal weight)
        {
            // Config
           var shippingFeeConfig = _configurationShippingServices.GetAll().OrderByDescending(_ => _.CreatedAt).FirstOrDefault();

            decimal baseFee = shippingFeeConfig.BaseFee; // Cước cơ bản
            decimal first5KmFee = shippingFeeConfig.First5KmFree;
            decimal additionalKmFee = shippingFeeConfig.AdditionalKmFee;
            decimal first10KgFee = shippingFeeConfig.First10KgFee;
            decimal additionalKgFee = shippingFeeConfig.AdditionalKgFee;

            // Tính phí quãng đường
            decimal distanceFee = distance <= 5 ? first5KmFee :
                first5KmFee + (distance - 5) * additionalKmFee;

            // Tính phí cân nặng
            decimal weightFee = weight <= 3 ? first10KgFee :
                first10KgFee + (weight - 3) * additionalKgFee;

            // Tổng phí
            return baseFee + distanceFee + weightFee;
        }

        public async Task<List<StoreDistance>> GetListStoreOrderbyDeliveryDistance(string deliveryAddress, List<Store> stores)
        {
            var baseUrl = _configuration["Distancematrix:BaseUrlGet"];
            var apiKey = _configuration["Distancematrix:APIKey"];
            var destinations = string.Join("|", stores.Select(s => $"{s.Latitude},{s.Longitude}"));

            var deliveryPosition = await ResponseLatitueLongtitueValue(deliveryAddress);

            var apiUrl = $"{baseUrl}?origins={deliveryPosition}&destinations={destinations}&key={apiKey}";
            var response = await _httpClient.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                return null; 
            }
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DistanceMatrixResponse>(responseContent);
            var distances = result.rows.First().elements
                .Select((element, index) => new StoreDistance { Store = stores[index], Distance = element.distance.value })
                .OrderBy(x => x.Distance)
                .ToList();

            return distances;
        }

        public async Task<string> ResponseLatitueLongtitueValue(string deliveryAddress)
        {
            var baseUrl = _configuration["GeoCodingAPI:BaseUrlGet"];
            var apiKey = _configuration["GeoCodingAPI:APIKey"];
            var apiUrl = $"{baseUrl}?q={deliveryAddress}&apiKey={apiKey}";
            var response = await _httpClient.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            var responseContent = await response.Content.ReadAsStringAsync();

            var jsonDocument = JsonDocument.Parse(responseContent);
            var position = jsonDocument.RootElement.GetProperty("items")[0].GetProperty("position");

            double lat = position.GetProperty("lat").GetDouble();
            double lng = position.GetProperty("lng").GetDouble();

            return $"{lat},{lng}";
        }

        public async Task<StoreDistance> GeStoreOrderbyDeliveryDistance(string deliveryAddress, Store store)
        {
            var baseUrl = _configuration["Distancematrix:BaseUrlGet"];
            var apiKey = _configuration["Distancematrix:APIKey"];
            var destinations = $"{store.Latitude}, {store.Longitude}"; 

            var deliveryPosition = await ResponseLatitueLongtitueValue(deliveryAddress);

            var apiUrl = $"{baseUrl}?origins={deliveryPosition}&destinations={destinations}&key={apiKey}";
            var response = await _httpClient.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DistanceMatrixResponse>(responseContent);
            var distances = result.rows.First().elements
                .Select((element, index) => new StoreDistance { Store = store, Distance = element.distance.value }).First();
            return distances;
        }
    }
}
public class DistanceMatrixResponse
{
    public List<string> destination_addresses { get; set; }
    public List<string> origin_addresses { get; set; }
    public List<Row> rows { get; set; }
    public string status { get; set; }
}

public class Row
{
    public List<Element> elements { get; set; }
}

public class Element
{
    public Distance distance { get; set; }
    public Duration duration { get; set; }
    public string origin { get; set; }
    public string destination { get; set; }
    public string status { get; set; }
}

public class Distance
{
    public string text { get; set; }
    public int value { get; set; } // Distance in meters
}

public class Duration
{
    public string text { get; set; }
    public int value { get; set; } // Duration in seconds
}