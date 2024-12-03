﻿using AutoMapper;
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
        Task<string> ResponseLatitueLongtitueValue(string deliveryAddress);
    }
    public class ShippingService : IShippingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ShippingService(HttpClient httpClient,  IMapper mapper, 
            IStoreService storeService, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
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