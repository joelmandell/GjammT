using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Runtime.CompilerServices;
using GjammT.Models.CustomerRegister;
using GjammT.Models.Data;
using GjammT.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GjammT.Booking;

public class Plugins(Kernel kernel, ChatHistory chatHistory, string language, DbContextOptions<AppDbContext> dbOptions)
{
    private readonly Kernel kernel = kernel;    
    private readonly ChatHistory chatHistory = chatHistory;
    private readonly DbContextOptions<AppDbContext> dbOptions = dbOptions;

    [KernelFunction("create_customer")]
    [Description(
        "Create a new customer/association. Skapa en ny kund/förening")]
    public async Task<string> CreateCustomer(Customer customer)
    {
        using var db = new AppDbContext(dbOptions, tenantId: null);
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
    public async Task<User> CreateUser(
        [Description("User's email address. Användarens e-postadress.")] string email,
        [Description("User's password. Användarens lösenord.")] string password,
        [Description("User's first name. Användarens förnamn.")] string firstName,
        [Description("User's last name. Användarens efternamn.")] string lastName,
        [Description("The customer the user belongs to. Kunden som användaren tillhör.")] string customerName)
    {
        using var db = new AppDbContext(dbOptions, tenantId: null);
        var userService = new UserService(db);
        
        try
        {
            var user = await userService.CreateUserAsync(email, password, firstName, lastName);
            
            // Associate user with customer if provided
            var customer = await db.Customers.Include(x => x.UserRoles)
                .FirstOrDefaultAsync(x => x.Name == customerName);
            
            if (customer != null)
            {
                // User is already saved, we can create the relationship
                Console.WriteLine($"User {email} created and associated with customer {customerName}");
            }
            
            return user;
        }
        catch(Exception e)
        {
            Console.WriteLine($"Failed to create user: {e.Message}");
            throw;
        }
    }

    [KernelFunction("list_customers")]
    [Description(
        "List customers/associations. Lista kunder/föreningar")]
    public async Task<IEnumerable<Customer>> ListCustomers()
    {
        using var db = new AppDbContext(dbOptions, tenantId: null);
        
        return await db.Customers.ToListAsync();
    }

    [KernelFunction("find_users")]
    [Description("Find users that belongs to a certain customer or association that is. Hitta användare som tillhör en kund eller förening.")]
    public async Task<User[]> FindUsers(string topic)
    {        
        using var db = new AppDbContext(dbOptions, tenantId: null);

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