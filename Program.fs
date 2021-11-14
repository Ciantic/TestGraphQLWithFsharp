open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Hosting
open Microsoft.EntityFrameworkCore 
open Microsoft.EntityFrameworkCore.Sqlite
open EntityFrameworkCore.FSharp.Extensions

[<CLIMutable>]
type Person =
    { Id : Guid
      FirstName : string
      LastName : string
      Address : string
      City : string}

type AppDbContext(opts) =
    inherit DbContext(opts)

    // member this.Persons : DbSet<Person> = System.Linq.Set<Person>()

    [<DefaultValue>]
    val mutable persons : DbSet<Person> 

    // override _.OnModelCreating builder =
    //     builder.RegisterOptionTypes() // enables option values for all entities

    member public this.Persons with get() = this.persons
                               and set p = this.persons <- p


type Author(name:string) =
    member this.Name = name;

type Book(title:string, author:Author) =
    member this.Title = title;
    member this.Author = author;

type Query() =
    member this.Book =
        new Book("F#", new Author("Jon"))

type Mutation() =
    member this.CreateBook (book:Book) =
        new Book("F#", new Author("Jon"))


[<EntryPoint>]
let main args =

    let builder = WebApplication.CreateBuilder(args)

    builder.Services
            .AddDbContext<AppDbContext>(fun opts -> opts.UseSqlite("Data Source=foo.sqlite") |> ignore)
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>() |> ignore

    let app = builder.Build()

    app.UseDeveloperExceptionPage() |> ignore
    app.UseRouting()
           .UseEndpoints(fun endpoints ->
               endpoints.MapGraphQL() |> ignore
            ) |> ignore
    app.MapGet("/", Func<string>(fun () -> "Hello World!")) |> ignore


    let createDb =
        use scope = app.Services.CreateScope()
        let db = scope.ServiceProvider.GetRequiredService<AppDbContext>()
        db.Database.EnsureDeleted() |> Console.WriteLine
        db.Database.EnsureCreated() |> Console.WriteLine

    createDb
    
    app.Run()

    0 // Exit code

