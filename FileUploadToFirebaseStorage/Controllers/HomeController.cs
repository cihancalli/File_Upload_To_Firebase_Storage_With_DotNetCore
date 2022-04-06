using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FileUploadToFirebaseStorage.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using Firebase.Auth;
using System.Threading;
using Firebase.Storage;

namespace FileUploadToFirebaseStorage.Controllers
{
    public class HomeController : Controller
    {
        private static string apiKey = "API_KEY";
        private static string Bucket = "APP_ID.appspot.com";
        private static string AuthEmail = "MAİL";
        private static string AuthPassword = "PASSWORD";

        private readonly IHostingEnvironment _env;

        public HomeController(IHostingEnvironment env)
        {
            _env = env;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(IFormFile file)
        {
            IFormFile fileupload = file;
            FileStream fs = null;

            var fileName = Path.GetFileName(fileupload.FileName);
            var fileExtension = Path.GetExtension(fileName);
            var newFileName = String.Concat(Convert.ToString(Guid.NewGuid()), fileExtension);
            if (fileupload.Length > 0)
            {
                //Upload the file to firebase
                string foldername = "firebaseFiles";
                string path = Path.Combine(_env.WebRootPath, $"images/{foldername}");



                if (Directory.Exists(path))
                {
                    using (fs = new FileStream(Path.Combine(path, fileupload.FileName), FileMode.Create))
                    {
                        await fileupload.CopyToAsync(fs);
                    }
                    fs = new FileStream(Path.Combine(path, fileupload.FileName), FileMode.Open);
                }
                else
                {
                    Directory.CreateDirectory(path);
                }

                //Firebase uploading stuff
                var auth = new FirebaseAuthProvider(new FirebaseConfig(apiKey));
                var a = await auth.SignInWithEmailAndPasswordAsync(AuthEmail, AuthPassword);

                // you can use CancellationTokenSource to cancel the upload midway
                var cancellation = new CancellationTokenSource();

                var upload = new FirebaseStorage(
                    Bucket,
                    new FirebaseStorageOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(a.FirebaseToken),
                        ThrowOnCancel = true
                    })
                    .Child("assets")
                    .Child($"{newFileName}")    //.{Path.GetExtension(fileupload.FileName).Substring(1)}
                    .PutAsync(fs, cancellation.Token);

                try
                {
                    ViewBag.link = await upload;
                    return Ok();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"***** {ex} *****");
                    throw;
                }

            }
            return View();
        }
    }
}
