using System;
using System.Text.Json;
using Microsoft.Win32;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure;
using Umbraco.Extensions;
using static System.Net.Mime.MediaTypeNames;

namespace Umbraco.Cms.Integrations.Search.Algolia.Services
{
    public class AlgoliaSearchPropertyIndexValueFactory : IAlgoliaSearchPropertyIndexValueFactory
    {
        private readonly IDataTypeService _dataTypeService;

        private readonly IMediaService _mediaService;

        private readonly IContentService _contentService;

        public AlgoliaSearchPropertyIndexValueFactory(IDataTypeService dataTypeService, IMediaService mediaService, IContentService contentService)
        {
            _dataTypeService = dataTypeService;
            _mediaService = mediaService;
            _contentService = contentService;

            Converters = new Dictionary<string, Func<KeyValuePair<string, IEnumerable<object>>, string>>
            {
                { Core.Constants.PropertyEditors.Aliases.MediaPicker3, ConvertMediaPicker }, // Register the converter for Media picker
                { Core.Constants.PropertyEditors.Aliases.MultiNodeTreePicker, ConvertMultiNodeTreePicker }, // Register the converter for MultiNodeTreePicker
                { Core.Constants.PropertyEditors.Aliases.Tags, ConvertTagsPicker } // Register the converter for Tag picker
            };
        }

        public Dictionary<string, Func<KeyValuePair<string, IEnumerable<object>>, string>> Converters { get; set; }

        public virtual KeyValuePair<string, string> GetValue(IProperty property, string culture)
        {
            var dataType = _dataTypeService.GetByEditorAlias(property.PropertyType.PropertyEditorAlias)
                .FirstOrDefault(p => p.Id == property.PropertyType.DataTypeId);

            if (dataType == null) return default;

            var indexValues = dataType.Editor.PropertyIndexValueFactory.GetIndexValues(property, culture, string.Empty, true);

            if (indexValues == null || !indexValues.Any()) return new KeyValuePair<string, string>(property.Alias, string.Empty);

            var indexValue = indexValues.First();

            if (Converters.ContainsKey(property.PropertyType.PropertyEditorAlias))
            {
                var result = Converters[property.PropertyType.PropertyEditorAlias].Invoke(indexValue);
                return new KeyValuePair<string, string>(property.Alias, result);
            }

            return new KeyValuePair<string, string>(indexValue.Key, ParseIndexValue(indexValue.Value));
        }

        public string ParseIndexValue(IEnumerable<object> values)
        {
            if (values != null && values.Any())
            {
                var value = values.FirstOrDefault();

                if (value == null) return string.Empty;

                return value.ToString();
            }

            return string.Empty;
        }

        private string ConvertMediaPicker(KeyValuePair<string, IEnumerable<object>> indexValue)
        {
            var list = new List<string>();

            var parsedIndexValue = ParseIndexValue(indexValue.Value);

            if (string.IsNullOrEmpty(parsedIndexValue)) return string.Empty;

            var inputMedia = JsonSerializer.Deserialize<IEnumerable<MediaItem>>(parsedIndexValue);

            if (inputMedia == null) return string.Empty;

            foreach (var item in inputMedia)
            {
                if (item == null) continue;

                var mediaItem = _mediaService.GetById(Guid.Parse(item.MediaKey));

                if (mediaItem == null) continue;

                list.Add(mediaItem.GetValue("umbracoFile")?.ToString() ?? string.Empty);
            }

            return JsonSerializer.Serialize(list);
        }

        private string ConvertMultiNodeTreePicker(KeyValuePair<string, IEnumerable<object>> indexValue)
        {
            var list = new List<string>();
            string[] resultIDs;
            var parsedIndexValue = ParseIndexValue(indexValue.Value);

            if (!String.IsNullOrEmpty(parsedIndexValue))
            {
                resultIDs = parsedIndexValue.ToString().Split(",");
                if (resultIDs.Length > 0)
                {
                    foreach (var udiAsString in resultIDs)
                    {
                        var udiSubString = udiAsString.Substring(udiAsString.Length - 32);
                        var guid = Guid.ParseExact(udiSubString, "N");
                        var node = _contentService.GetById(guid);
                        if (node!=null)
                        {
                            list.Add(node.Name);
                        }

                    }
                }
            }

            return string.Join(",", list);

        }

        public String ConvertTagsPicker(KeyValuePair<string, IEnumerable<object>> indexValue)
        {
            var list = new List<string>();
            IEnumerable<object> tagsList = indexValue.Value;
            string concatenatedTags = string.Join(",", tagsList.Select(obj => obj.ToString()));

            return concatenatedTags;
        }

    }
}
