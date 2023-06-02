using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace first_challenge_21
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class Comment
    {
        public string Text { get; set; }
    }
    
    public class Startup
    {
        private static readonly List<User> users = new List<User>
        {
            new User { Id = 1, Name = "Alice", Email = "alice@example.com", Password = "password1" },
            new User { Id = 2, Name = "Bob", Email = "bob@example.com", Password = "password2" },
            new User { Id = 3, Name = "Charlie", Email = "charlie@example.com", Password = "password3" }
        };

        private static readonly List<Comment> comments = new List<Comment>
        {
            new Comment { Text = "This is a comment" }
        };
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSession(options =>
            {
                options.Cookie.HttpOnly = false;
            });
            services.AddDistributedMemoryCache();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSession();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            app.Run(async (context) =>
            {
                if (context.Request.Path == "/")
                {
                    await context.Response.WriteAsync(@"
                        <h1>Welcome to the Store</h1>
                        <form action=""/login"" method=""post"">
                            <label for=""email"">Email:</label>
                            <input type=""email"" name=""email"" id=""email"" required>
                            <label for=""password"">Password:</label>
                            <input type=""password"" name=""password"" id=""password"" required>
                            <button type=""submit"">Login</button>
                        </form>
                    ");
                }
                else if (context.Request.Path == "/login" && context.Request.Method == "POST")
                {
                    var email = context.Request.Form["email"];
                    var password = context.Request.Form["password"];
                    var user = users.Find(u => u.Email == email && u.Password == password);

                    if (user != null)
                    {
                        context.Session.SetInt32("UserId", user.Id);
                        context.Response.Redirect($"/user/{user.Id}");
                    }
                    else
                    {
                        await context.Response.WriteAsync("Invalid credentials. Please try again.");
                    }
                }
                else if (context.Request.Path.StartsWithSegments("/user") && context.Request.Method == "GET")
                {
                    var id = int.Parse(context.Request.Path.Value.Split("/")[2]);
                    var user = users.Find(u => u.Id == id);

                    if (user != null)
                    {
                        var commentsHtml = string.Join("", comments.ConvertAll(c => $"<li>{c.Text}</li>"));

                        await context.Response.WriteAsync($@"
                            <h1>User Profile</h1>
                            <p>Name: {user.Name}</p>
                            <p>Email: {user.Email}</p>
                            <h2>Write a comment:</h2>
                            <form action=""/comments"" method=""post"">
                                <input type=""text"" name=""comment"" id=""comment"">
                                <button type=""submit"">Send</button>
                            </form>
                            <h2>Comments:</h2>
                            <ul>
                                {commentsHtml}
                            </ul>
                        ");
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("User not found");
                    }
                }
                else if (context.Request.Path == "/comments" && context.Request.Method == "POST")
                {
                    if (context.Session.TryGetValue("UserId", out var userIdBytes))
                    {
                        var userId = BitConverter.ToInt32(userIdBytes.Reverse().ToArray());
                        var commentText = context.Request.Form["comment"];
                        Console.WriteLine($"Received comment: {commentText}");
                        comments.Add(new Comment { Text = commentText });

                        // Redirects to the user profile page
                        context.Response.Redirect($"/user/{userId}");
                    }
                    else
                    {
                        context.Response.Redirect("/");
                    }
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Page not found");
                }
            });
        }
    }
}
