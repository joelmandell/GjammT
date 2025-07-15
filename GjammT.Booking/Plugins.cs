using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Runtime.CompilerServices;
using GjammT.Models.CustomerRegister;
using GjammT.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GjammT.Booking;

public class Plugins(Kernel kernel, ChatHistory chatHistory, string language)
{
    private readonly Kernel kernel = kernel;    
    private readonly ChatHistory chatHistory = chatHistory;

    [KernelFunction("create_customer")]
    [Description(
        "Create a new customer/association. Skapa en ny kund/förening")]
    public async Task<string> CreateCustomer(Customer customer)
    {
        using var db = new AppDbContext();
        db.Customers.Add(customer);
        try
        {
            await db.SaveChangesAsync();
        }
        catch(Exception e)
        {
            Console.WriteLine(e.ToString());
            return $"Failed with error: {e.Message}";
        }
        
        return $"Kund med namn {customer.Name} har skapats.";
    }
    
    
    [KernelFunction("create_user")]
    [Description(
"Create a new user. Skapa en ny användare")]
    public async Task<User> CreateUser(User user, [Description("The customer the user belongs to. Kunden som användaren tillhör.")] string customerName)
    {
        using var db = new AppDbContext();
        var customer = await db.Customers.Include(x => x.UserRoles).FirstOrDefaultAsync(x => x.Name == customerName && x.UserRoles.Any(u => u.UserId == user.Id));
        // user.CustomerRoles.Add(customer.);
        
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync();
        }
        catch(Exception e)
        {
            Console.WriteLine(e.ToString());
        }
        return user;
    }

    [KernelFunction("list_customers")]
    [Description(
        "List customers/associations. Lista kunder/föreningar")]
    public async Task<IEnumerable<Customer>> ListCustomers()
    {
        using var db = new AppDbContext();
        
        return await db.Customers.ToListAsync();
    }

    [KernelFunction("find_users")]
    [Description("Find users that belongs to a certain customer or association that is. Hitta användare som tillhör en kund eller förening.")]
    public async Task<IEnumerable<User>> FindUsers(string topic)
    {        
        using var db = new AppDbContext();

        var customer = await db.Customers.Select(x => new {x.Id,x.Name}).FirstOrDefaultAsync(x => x.Name == topic);

        return await db.Users.Where(x => x.CustomerRoles.Any(r => r.CustomerId == customer.Id) ).ToListAsync(); ;    
    }

    [KernelFunction("email_to_users")]
    [Description("Send email to specified users. Skicka mejl till specificerade användare.")]
    public async Task<string> SendEmail(string subject, string message, string[] users)
    {
        foreach (var user in users)
        {
            Console.WriteLine($"Sending email to {user} with {subject}");
        }
        return "All mails are sent";
    }
}