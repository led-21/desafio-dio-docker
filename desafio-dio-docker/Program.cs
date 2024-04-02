using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddDbContext<MyContext>(options =>
        options.UseSqlServer(builder.Configuration["ConnectionStrings"]));

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

var todosApi = app.MapGroup("/todos");

todosApi.MapGet("/", (MyContext db) => {
    return Results.Ok(db.Todos);
});

todosApi.MapGet("/{id}", (int id, MyContext db) =>
    db.Todos.FirstOrDefault(a => a.Id == id) is {} todo
        ? Results.Ok(todo)
        : Results.NotFound());


todosApi.MapPost("/", async (MyContext db, Todo todo) =>
{
    db.Todos.Add(new(null, todo.Title, todo.DueBy, todo.IsComplete));
    await db.SaveChangesAsync();

    return Results.Created($"/todos/{todo.Id}", todo);
});

todosApi.MapPut("/{id}", async (int id, Todo inputTodo, MyContext db) =>
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null) 
        return Results.NotFound();

    todo.Title = inputTodo.Title;
    todo.IsComplete = inputTodo.IsComplete;
    await db.SaveChangesAsync();

    return Results.NoContent();
});

todosApi.MapDelete("/{id}", async (MyContext db, int id) => {
    if (db.Todos.FirstOrDefault(a => a.Id == id) is {} todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
    
});

app.Run();

public record Todo(int? Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false)
{
    public string? Title { get; set; } = Title;
    public bool IsComplete { get; set; } = IsComplete;
};

[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}

class MyContext : DbContext
{
    public DbSet<Todo> Todos { get; set; }

    public MyContext(DbContextOptions<MyContext> options) : base(options)
    {

    }
}