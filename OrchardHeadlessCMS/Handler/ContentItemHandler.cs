﻿using OrchardCore;
using OrchardCore.ContentManagement;
using OrchardHeadlessCMS.Models;
using System.Text;
using System.Text.Json;

namespace OrchardHeadlessCMS.Handler
{
    public class ContentItemHandler
    {
        private readonly IOrchardHelper _orchardHelper;
        private readonly JsonSerializerOptions _options;
        private IContentManager? ContentManager;
        private IContentItemIdGenerator? ContentItemIdGenerator;
        public ContentItemHandler(IOrchardHelper orchardHelper)
        {
            _orchardHelper = orchardHelper;
            _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            ContentManager = _orchardHelper.HttpContext.RequestServices.GetService<IContentManager>();
            ContentItemIdGenerator = _orchardHelper.HttpContext.RequestServices.GetService<IContentItemIdGenerator>();
        }

        public async Task CreateContentItem(string? type, string? text, string? author)
        {
            var contentItem = new ContentItem
            {
                ContentType = type,
                DisplayText = text,
                Author = author,
                Owner = "",
                PublishedUtc = DateTime.UtcNow,
                CreatedUtc = DateTime.UtcNow,
                ModifiedUtc = DateTime.UtcNow,
            };
            contentItem.ContentItemId = ContentItemIdGenerator?.GenerateUniqueId(contentItem);
            await ContentManager.CreateAsync(contentItem);
        }

        public async Task<ItemContent> GetSingleAsync(string? Id)
        {
            var contentItem = await _orchardHelper.GetContentItemByIdAsync(Id);
            return new ItemContent
            {
                Author = contentItem?.Author,
                Content = JsonSerializer.Deserialize<Content?>(contentItem?.Content.ToString(), _options),
                DisplayText = contentItem?.DisplayText,
                ContentType = Helper.StringExtensions.AddSpacesToSentence(contentItem.ContentType, true)
            };
        }

        public async Task<List<ItemContent>?> GetListByTypeAsync(string? type)
        {
            var query = await _orchardHelper.QueryContentItemsAsync(q => q.Where(c => c.ContentType == type && c.Published == true));
            var roundedCount = Math.Round(query.Count() / 10M, MidpointRounding.ToPositiveInfinity);
            var result = query.ToList();
            if (result != null)
            {
                var ContentItems = new List<ItemContent>();
                foreach (var contentItem in result)
                {
                    ContentItems.Add(new ItemContent
                    {
                        Author = contentItem.Author,
                        DisplayText = contentItem.DisplayText,
                        ContentItemId = contentItem.ContentItemId,
                        ContentType = Helper.StringExtensions.AddSpacesToSentence(contentItem.ContentType, true),
                        Owner = contentItem.Owner,
                        CreatedUtc = contentItem.CreatedUtc,
                        ModifiedUtc = contentItem.ModifiedUtc,
                        PublishedUtc = contentItem.PublishedUtc,
                        Content = JsonSerializer.Deserialize<Content?>(contentItem?.Content.ToString(), _options)
                    });
                }
                return ContentItems;
            }
            return null;
        }

        public async Task<Content?> GetFirstByTypeAsync(string? type)
        {
            var getContentItem = await _orchardHelper.GetRecentContentItemsByContentTypeAsync(type, 1);
            var contentItem = getContentItem.FirstOrDefault();
            if (contentItem != null)
                return JsonSerializer.Deserialize<Content?>(contentItem?.Content.ToString(), _options);
            else
                return null;
        }

        //public static partial class JsonSerializerExtensions
        //{
        //    public static T? DeserializeAnonymousType<T>(string json, T anonymousTypeObject, JsonSerializerOptions? options = default)
        //        => JsonSerializer.Deserialize<T>(json, options);

        //    public static ValueTask<TValue?> DeserializeAnonymousTypeAsync<TValue>(Stream stream, TValue anonymousTypeObject, JsonSerializerOptions? options = default, CancellationToken cancellationToken = default)
        //        => JsonSerializer.DeserializeAsync<TValue>(stream, options, cancellationToken); // Method to deserialize from a stream added for completeness
        //}
    }
}