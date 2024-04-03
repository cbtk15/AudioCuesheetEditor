//This file is part of AudioCuesheetEditor.

//AudioCuesheetEditor is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//AudioCuesheetEditor is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with Foobar.  If not, see
//<http: //www.gnu.org/licenses />.
using AudioCuesheetEditor;
using AudioCuesheetEditor.Data.Options;
using AudioCuesheetEditor.Data.Services;
using AudioCuesheetEditor.Extensions;
using AudioCuesheetEditor.Model.IO.Audio;
using AudioCuesheetEditor.Model.UI;
using AudioCuesheetEditor.Model.Utility;
using BlazorDownloadFile;
using Blazorise;
using Blazorise.Bootstrap5;
using Howler.Blazor.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Toolbelt.Blazor.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddBlazorise(options =>
{
    options.Debounce = true;
    options.DebounceInterval = 300;
})
.AddBootstrap5Providers();

builder.Services.AddScoped<IHowl, Howl>();
builder.Services.AddScoped<IHowlGlobal, HowlGlobal>();

builder.Services.AddBlazorDownloadFile();

builder.Services.AddScoped<LocalStorageOptionsProvider>();
builder.Services.AddScoped<MusicBrainzDataProvider>();

builder.Services.AddScoped<SessionStateContainer>();
builder.Services.AddScoped<TraceChangeManager>();
builder.Services.AddScoped<DateTimeUtility>();

builder.Services.AddLogging();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

builder.Services.AddHotKeys2();

var host = builder.Build();

await host.SetDefaultCulture();

await builder.Build().RunAsync();
