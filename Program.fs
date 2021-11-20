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

type Mutation() =
    member this.CreatePerson(input: {|
        firstName: String
        lastName: String
    |}, [<Service>] dbContext: AppDbContext) =
        let mutable person = Person(
            FirstName = input.firstName,
            LastName = input.lastName
        )
        dbContext.Persons.Add(person) |> ignore
        dbContext.SaveChanges() |> ignore
        person

[<EntryPoint>]
let main args =

    let builder = WebApplication.CreateBuilder(args)
    builder.Services
            .AddDbContext<AppDbContext>(
                fun opts -> opts.UseSqlite("Data Source=foo.sqlite") |> ignore
            )
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
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
        db.Persons.Add(Person(Address = "Fooland 123", City = "FooCity", FirstName = "John", LastName = "Doe")) |> ignore
        db.Persons.Add(Person(Address = "Fooland 123", City = "FooCity", FirstName = "Jack", LastName = "Doe")) |> ignore
        db.SaveChanges() |> ignore

    createDb
    
    app.Run()

    0 // Exit code


(*

{
  persons(first: 15, order: {firstName: ASC}, where: { firstName: {
    startsWith: "John"
  }}) {
    nodes {
      id,
      firstName
    }
  } 
}   

mutation {
  createPerson(input: {
    firstName: "Mary",
    lastName: "Doe"
  }) {
    id,
    firstName,
    lastName
  }
}

*)