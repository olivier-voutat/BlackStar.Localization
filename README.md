# BlackStar.Localization
Custom Localization for AspNetCore 2.0 (using Microsoft SQL database). Can be easily adapted to include other sources.

In AspNet Core, there is a wonderful localization system already available.

https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization

Only flaw, it is meant to be used only with Resource files embedded in the application.

I did this library to be able to use resources from a database table instead simply using a SqlDataReader (System.Data.SqlClient).

The only difference to use this one over the regular system is to replace in the startup file this:

            services.AddLocalization(o => o.ResourcesPath = "Resources");

By this:

            services.AddCustomLocalization(o =>
            {
                o.SourceArgs = new Dictionary<string, string>()
                {
                    { "ConnectionString", "yourConnectionString" },
                    { "Table", "yourDatabaseTable" },
                    { "Column", "yourTableColumnWhereToGetTheResource" }
                };
            });

In the code, by default, it expects the database table to have a "CultureName" and a "RersourceKey" columns.

It works for IHtmlLocalizer, IViewLocalizer, DataAnnotations... it really just replaces the regular system.

And if you want to use another system (JSON, XML, something else), it can be easily modified.

Create a new class that implements the IDataManager interface and modify the "CustomStringLocalizerFactory". In the "CreateCustomStringLocalizer" function, pass your new DataManager as parameter and you're done.

If you need different parameters to be passed from your application to the class library, you can send whatever you want. It is a regular dictionnary. You can freely use the provided "SqlDataManager" as inspiration to see how to use it.

Enjoy it ;)
