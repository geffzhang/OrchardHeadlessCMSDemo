using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardHeadlessCMS.Handler;
using OrchardHeadlessCMS.Models;

namespace OrchardHeadlessCMS.Pages
{
    public class ReviewsModel : PageModel
    {
        private readonly ContentItemHandler _handler;
        public ReviewsModel(ContentItemHandler handler)
        {
            _handler = handler;
        }

        public List<ItemContent>? Reviews { get; set; } = new();
        public ContentTypeDefinition ContentTypeDefinition { get; set; }
        
        public async Task OnGetAsync()
        {
            ContentTypeDefinition = _handler.GetTypeAsync("Reviews");
            Reviews = await _handler.GetListByTypeAsync("Reviews");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await _handler.PostReview("Reviews", Request.Form["summary"], Request.Form["comment"], Request.Form["author"], Convert.ToDecimal(Request.Form["stars"]));
            return RedirectToPage("Reviews");
        }
    }
}
