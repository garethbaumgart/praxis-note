using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PraxisNote.Application.Features.Tasks;
using PraxisNote.Web.Mcp;

namespace PraxisNote.Web.Mcp.Tools;

[McpServerToolType]
public sealed class TaskTools(McpUserContext userContext)
{
    [McpServerTool, Description("List all tasks for the current user. Returns tasks with their status, priority, due date, tags, and comments.")]
    public async Task<string> ListTasks(
        GetUserTasks getUserTasks,
        [Description("Include archived/completed tasks older than threshold")] bool includeArchived = false)
    {
        var query = new GetUserTasks.Query(userContext.UserId, userContext.ProfileId, includeArchived);
        var tasks = await getUserTasks.ExecuteAsync(query);
        return JsonSerializer.Serialize(tasks);
    }

    [McpServerTool, Description("Create a new task on the board.")]
    public async Task<string> CreateTask(
        CreateTask createTask,
        [Description("The title/description of the task")] string title)
    {
        var command = new CreateTask.Command(userContext.UserId, userContext.ProfileId, title);
        var result = await createTask.ExecuteAsync(command);
        return JsonSerializer.Serialize(result);
    }

    [McpServerTool, Description("Update the title of an existing task.")]
    public async Task<string> UpdateTask(
        UpdateTask updateTask,
        [Description("The ID of the task to update")] string taskId,
        [Description("The new title for the task")] string title)
    {
        var command = new UpdateTask.Command(Guid.Parse(taskId), userContext.UserId, title);
        var success = await updateTask.ExecuteAsync(command);
        return JsonSerializer.Serialize(new { success });
    }

    [McpServerTool, Description("Change the status of a task. Valid statuses: Todo, InProgress, Done.")]
    public async Task<string> ChangeTaskStatus(
        ChangeTaskStatus changeStatus,
        [Description("The ID of the task")] string taskId,
        [Description("Target status: Todo, InProgress, or Done")] string status)
    {
        var command = new ChangeTaskStatus.Command(Guid.Parse(taskId), userContext.UserId, status);
        var success = await changeStatus.ExecuteAsync(command);
        return JsonSerializer.Serialize(new { success });
    }

    [McpServerTool, Description("Toggle the priority flag on a task (high priority on/off).")]
    public async Task<string> ToggleTaskPriority(
        ToggleTaskPriority togglePriority,
        [Description("The ID of the task")] string taskId)
    {
        var command = new ToggleTaskPriority.Command(Guid.Parse(taskId), userContext.UserId);
        var success = await togglePriority.ExecuteAsync(command);
        return JsonSerializer.Serialize(new { success });
    }

    [McpServerTool, Description("Delete a task permanently.")]
    public async Task<string> DeleteTask(
        DeleteTask deleteTask,
        [Description("The ID of the task to delete")] string taskId)
    {
        var command = new DeleteTask.Command(Guid.Parse(taskId), userContext.UserId);
        var success = await deleteTask.ExecuteAsync(command);
        return JsonSerializer.Serialize(new { success });
    }

    [McpServerTool, Description("Set or clear the due date on a task. Pass null/empty date to clear.")]
    public async Task<string> SetTaskDueDate(
        SetDueDate setDueDate,
        ClearDueDate clearDueDate,
        [Description("The ID of the task")] string taskId,
        [Description("Due date in yyyy-MM-dd format, or empty string to clear")] string date)
    {
        if (string.IsNullOrWhiteSpace(date))
        {
            var clearCommand = new ClearDueDate.Command(Guid.Parse(taskId), userContext.UserId);
            var cleared = await clearDueDate.ExecuteAsync(clearCommand);
            return JsonSerializer.Serialize(new { success = cleared });
        }

        var setCommand = new SetDueDate.Command(Guid.Parse(taskId), userContext.UserId, DateOnly.Parse(date));
        var success = await setDueDate.ExecuteAsync(setCommand);
        return JsonSerializer.Serialize(new { success });
    }

    [McpServerTool, Description("Add or remove a tag from a task.")]
    public async Task<string> ManageTaskTag(
        AddTagToTask addTag,
        RemoveTagFromTask removeTag,
        [Description("The ID of the task")] string taskId,
        [Description("The ID of the tag")] string tagId,
        [Description("Action: 'add' or 'remove'")] string action)
    {
        var parsedTaskId = Guid.Parse(taskId);
        var parsedTagId = Guid.Parse(tagId);

        if (action.Equals("add", StringComparison.OrdinalIgnoreCase))
        {
            await addTag.ExecuteAsync(new AddTagToTask.Command(userContext.UserId, parsedTaskId, parsedTagId));
            return JsonSerializer.Serialize(new { success = true, action = "added" });
        }

        await removeTag.ExecuteAsync(new RemoveTagFromTask.Command(userContext.UserId, parsedTaskId, parsedTagId));
        return JsonSerializer.Serialize(new { success = true, action = "removed" });
    }
}
