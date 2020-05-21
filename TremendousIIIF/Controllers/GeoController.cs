﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using TremendousIIIF.Common.Configuration;

namespace TremendousIIIF.Controllers
{
    public class GeoController : Controller
    {
        private readonly ImageServer Conf;
        private readonly ImageProcessing.ImageProcessing Processor;
        private readonly ILogger<GeoController> _log;
        private readonly IHttpClientFactory _httpClientFactory;
        public GeoController(IHttpClientFactory httpClientFactory, ILogger<GeoController> log, ImageProcessing.ImageProcessing processor, ImageServer conf)
        {
            Processor = processor;
            _log = log;
            _httpClientFactory = httpClientFactory;
            Conf = conf;
        }
        [Produces("application/geo+json-seq", "application/json")]
        [HttpGet("/{id}/geo.json", Name = "geo.json")]
        public async Task<ActionResult<GeoJSON.Net.Feature.Feature>> GetGeoJson(string naan, string id)
        {
            var imageUri = new Uri(new Uri(Conf.Location), id);
            var data = await Processor.GetImageGeoData(imageUri, Conf.GeoDataPath);

            return data;

        }
    }
}