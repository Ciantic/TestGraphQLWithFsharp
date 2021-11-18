open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.EntityFrameworkCore 
open HotChocolate.Types 
open HotChocolate.Data
open HotChocolate;

// [<CLIMutable>]
// type Person =
//     { Id : Guid
//       FirstName : string
//       LastName : string
//       Address : string
//       City : string}

type Person() =
    member val Id = Guid.NewGuid() with get, set
    member val FirstName = "" with get, set
    member val LastName = "" with get, set
    member val Address = "" with get, set
    member val City = "" with get, set

type AppDbContext(opts) =
    inherit DbContext(opts)
    member this.Persons = this.Set<Person>()

type Query() =
    [<UsePaging>]
    [<UseProjection>]
    [<UseFiltering>]
    [<UseSorting>]
    member this.Persons ([<Service>] dbContext: AppDbContext) =
        dbContext.Persons

[<EntryPoint>]
let main args =

    let builder = WebApplication.CreateBuilder(args)
    builder.Services
            .AddDbContext<AppDbContext>(
                fun opts -> opts.UseSqlite("Data Source=foo.sqlite") |> ignore
            )
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddFiltering()
            .AddSorting()
            .AddProjections() |> ignore

    let app = builder.Build()
    app.UseDeveloperExceptionPage() |> ignore
    app.UseRouting() |> ignore
    app.MapGraphQL() |> ignore

    let createDb =
        use scope = app.Services.CreateScope()
        let db = scope.ServiceProvider.GetRequiredService<AppDbContext>()
        db.Database.EnsureDeleted() |> Console.WriteLine
        db.Database.EnsureCreated() |> Console.WriteLine

    createDb
    
    app.Run()

    0 // Exit code

