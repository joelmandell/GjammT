using System.ComponentModel;
using System.Data;
using GjammT.Models.CustomerRegister;
using Microsoft.SemanticKernel;

namespace GjammT.Booking;

public class Plugins
{
    [KernelFunction("find_users")]
    [Description("Find users that belongs to a certain customer or association that is.")]
    public async Task<IEnumerable<User>> FindUsers(string topic)
    {        
        
        return new List<User>() { new User
            {
                Username = null,
                Email = "joelmandell@mejl.se",
                Password = null,
                FirstName = "Joel",
                LastName = "Mandell",
                PhoneNumber = null
            }
        };    
    }
}