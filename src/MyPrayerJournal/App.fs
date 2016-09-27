
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Localization
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open System
open System.IO

/// Startup class for myPrayerJournal
type Startup(env : IHostingEnvironment) =
  
  /// Configuration for this application
  member this.Configuration =
    let builder =
      ConfigurationBuilder()
        .SetBasePath(env.ContentRootPath)
        .AddJsonFile("appsettings.json", optional = true, reloadOnChange = true)
        .AddJsonFile(sprintf "appsettings.%s.json" env.EnvironmentName, optional = true)
    // For more details on using the user secret store see https://go.microsoft.com/fwlink/?LinkID=532709
    match env.IsDevelopment () with true -> ignore <| builder.AddUserSecrets () | _ -> ()
    ignore <| builder.AddEnvironmentVariables ()
    builder.Build ()

  // This method gets called by the runtime. Use this method to add services to the container.
  member this.ConfigureServices (services : IServiceCollection) =
    ignore <| services.AddOptions ()
    ignore <| services.Configure<AppConfig>(this.Configuration.GetSection("MyPrayerJournal"))
    ignore <| services.AddLocalization (fun options -> options.ResourcesPath <- "Resources")
    ignore <| services.AddMvc ()
    ignore <| services.AddDistributedMemoryCache ()
    ignore <| services.AddSession ()
    // RethinkDB connection
    async {
      let cfg = services.BuildServiceProvider().GetService<IOptions<AppConfig>>().Value
      let! conn = DataConfig.Connect cfg.DataConfig
      do! conn.EstablishEnvironment cfg
      ignore <| services.AddSingleton conn
    } |> Async.RunSynchronously

  // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
  member this.Configure (app : IApplicationBuilder, env : IHostingEnvironment, loggerFactory : ILoggerFactory) =
    ignore <| loggerFactory.AddConsole(this.Configuration.GetSection "Logging")
    ignore <| loggerFactory.AddDebug ()

    match env.IsDevelopment () with
    | true -> ignore <| app.UseDeveloperExceptionPage ()
              ignore <| app.UseBrowserLink ()
    | _ -> ignore <| app.UseExceptionHandler("/error")

    ignore <| app.UseStaticFiles ()

    // Add external authentication middleware below. To configure them please see https://go.microsoft.com/fwlink/?LinkID=532715

    ignore <| app.UseMvc(fun routes ->
      ignore <| routes.MapRoute(name = "default", template = "{controller=Home}/{action=Index}/{id?}"))

/// Default to Development environment
let defaults = seq { yield WebHostDefaults.EnvironmentKey, "Development" }
               |> dict

[<EntryPoint>]
let main argv =
  let cfg =
    ConfigurationBuilder()
      .AddInMemoryCollection(defaults)
      .AddEnvironmentVariables("ASPNETCORE_")
      .AddCommandLine(argv)
      .Build()
      
  WebHostBuilder()
    .UseConfiguration(cfg)
    .UseKestrel()
    .UseContentRoot(Directory.GetCurrentDirectory())
    .UseStartup<Startup>()
    .Build()
    .Run()
  0