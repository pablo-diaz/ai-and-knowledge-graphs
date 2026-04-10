using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using WebAppForWorkflows.Services;

namespace WebAppForWorkflows.Pages;

public class IndexModel : PageModel
{
    private readonly WorkflowSessionsHub _hub;

    [BindProperty]
    public WorkflowSessionsHub.Session UserSession { get; set; }

    [BindProperty]
    public InteractingState State { get; set; }

    [BindProperty]
    public string Question { get; set; }


    public enum InteractingState
    {
        ASKING,
        ANSWERING
    }

    public IndexModel(WorkflowSessionsHub hub)
    {
        UserSession = WorkflowSessionsHub.Session.New();
        State = InteractingState.ASKING;
        this._hub = hub;
    }

    public void OnGet()
    {
    }

    public Task<IActionResult> OnPostAskQuestionAsync()
    {
        UserSession = _hub.Ask(Question);
        State = InteractingState.ANSWERING;
        return Task.FromResult(Page() as IActionResult);
    }

}
