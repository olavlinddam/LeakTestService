using System.ComponentModel.DataAnnotations;

namespace LeakTestService.Models.DTOs;

public class LeakTestDTO
{
    public LeakTest? LeakTest { get; set; }
    
    [Required] public string Identifier { get; set; }
}