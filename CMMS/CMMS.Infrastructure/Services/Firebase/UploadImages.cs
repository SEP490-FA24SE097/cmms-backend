using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;


namespace CMMS.Infrastructure.Services.Firebase;

public static class UploadImages
{
   public static async Task<List<string>> UploadFile(List<IFormFile> file)
    {
        var bucketName = "ccms-d6bf2.firebasestorage.app";
        GoogleCredential credential =
            GoogleCredential.FromFile("firebase.json");
        var storage = StorageClient.Create(credential);
        List<string> images = [];
        foreach (var item in file)
        {
            var objectName = $"{Path.GetRandomFileName()}_{item.FileName}";

            using (var stream = item.OpenReadStream())
            {
                await storage.UploadObjectAsync(bucketName, objectName, null, stream);
            }
            var publicUrl = $"https://storage.googleapis.com/{bucketName}/{objectName}";
            images.Add(publicUrl);
        }

        return images;
    }
}

