namespace GuidedProject.ApiApp.Models;

public record PromptRequest(string Prompt);
public record PromptWithRoleRequest(string Role, string Content);
