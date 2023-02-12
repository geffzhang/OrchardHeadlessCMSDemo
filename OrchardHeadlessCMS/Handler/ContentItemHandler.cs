﻿using Markdig;
using OrchardCore;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.Markdown.Models;
using OrchardCore.Title.Models;
using OrchardHeadlessCMS.Models;
using System.Text;
using System.Text.Json;

namespace OrchardHeadlessCMS.Handler
{
    public class ContentItemHandler
    {
        private readonly JsonSerializerOptions _options;
        private readonly IOrchardHelper _orchardHelper;
        private readonly IContentManager? _contentManager;
        private readonly IContentItemIdGenerator? _contentItemIdGenerator;
        private readonly IContentDefinitionManager? _contentDefinitionManager;
        public ContentItemHandler(IOrchardHelper orchardHelper)
        {
            _orchardHelper = orchardHelper;
            _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _contentManager = _orchardHelper.HttpContext.RequestServices.GetService<IContentManager>();
            _contentItemIdGenerator = _orchardHelper.HttpContext.RequestServices.GetService<IContentItemIdGenerator>();
            _contentDefinitionManager = _orchardHelper.HttpContext.RequestServices.GetService<IContentDefinitionManager>();
        }

        public async Task CreateContentItem(string? type,string? summary, string? text, string? author)
        {
            var contentItem = await _contentManager.NewAsync(type);
            contentItem.Author= author;
            contentItem.DisplayText = summary;
            var titlePart = contentItem.As<TitlePart>();             
            titlePart.Title = summary;
            titlePart.Apply();
            var markdownPart = contentItem.As<MarkdownBodyPart>();
            markdownPart.Markdown = text;
            markdownPart.Apply();
            await _contentManager.CreateAsync(contentItem);
        }

        public async Task<ItemContent> GetSingleAsync(string? Id)
        {
            var contentItem = await _orchardHelper.GetContentItemByIdAsync(Id);
            return new ItemContent
            {
                Author = contentItem?.Author,
                Content = JsonSerializer.Deserialize<Content?>(contentItem?.Content.ToString(), _options),
                DisplayText = contentItem?.DisplayText,
                ContentType = contentItem?.ContentType
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
                        ContentType = contentItem.ContentType,
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
        public async Task<ContentTypeDefinition?> GetTypeAsync(string? type)
        {
            var getContentType = _contentDefinitionManager.GetTypeDefinition(type);
            return getContentType ?? null;
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
