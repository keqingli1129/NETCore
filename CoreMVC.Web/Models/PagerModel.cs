namespace CoreMVC.Web.Models;

/// <summary>
/// View model for the shared <c>_Pager</c> partial. Renders a windowed pager
/// (« first, ‹ prev, a few numbers around the current page, › next, » last)
/// that links back to the current controller's <c>Index</c> action.
/// </summary>
public record PagerModel(int PageNumber, int TotalPages, int PageSize);
