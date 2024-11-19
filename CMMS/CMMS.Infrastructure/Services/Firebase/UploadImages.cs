using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace CMMS.Infrastructure.Services.Firebase;

public static class UploadImages
{
    public static async Task<List<string>> UploadFile(List<IFormFile> file)
    {
        var bucketName = "ccms-d6bf2.firebasestorage.app";
        GoogleCredential credential =
            GoogleCredential.FromFile("https://api.jsonbin.io/v3/b/673af944acd3cb34a8aa6440?meta=false");
        var storage = StorageClient.Create(credential);
        List<string> images = [];
        foreach (var item in file)
        {
            var objectName = $"{Path.GetRandomFileName()}_{item.FileName}";

            using (var stream = item.OpenReadStream())
            {
                await storage.UploadObjectAsync(bucketName, objectName, null, stream);
            }
            var publicUrl = $"https://storage.googleapis.com/v0/b/{bucketName}/o/{objectName}?alt=media";
            images.Add(publicUrl);
        }

        return images;
    }

    public static async Task<List<string>> UploadToFirebase(List<string> file)
    {
        HttpClient client = new HttpClient();
        HttpResponseMessage response = await client.GetAsync("https://api.jsonbin.io/v3/b/673af944acd3cb34a8aa6440?meta=false");
        response.EnsureSuccessStatusCode();
        string jsonStrring = await response.Content.ReadAsStringAsync();
        var bucketName = "ccms-d6bf2.firebasestorage.app";
        GoogleCredential credential =
            GoogleCredential.FromJson(jsonStrring);
        var storage = StorageClient.Create(credential);
        List<string> images = [];
        foreach (var base64Image in file)
        {
            try
            {
                // Decode the base64 string
                var fileBytes = Convert.FromBase64String(base64Image);
                // Generate a unique file name
                var fileName = $"{Guid.NewGuid()}.jpg"; // Assuming JPG format
                // Upload to Firebase Storage
                using (var memoryStream = new MemoryStream(fileBytes))
                {
                    await storage.UploadObjectAsync(bucketName, fileName, "image/jpeg", memoryStream);

                    // Generate the public URL
                    var publicUrl = $"https://firebasestorage.googleapis.com/v0/b/{bucketName}/o/{fileName}?alt=media";
                    images.Add(publicUrl);
                }
            }
            catch (FormatException ex)
            {

            }
        }

        return images;
    }

}

