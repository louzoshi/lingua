using System.ComponentModel.DataAnnotations;

namespace Lingua.ViewModels.Classrooms;

public class EnrollViewModel
{
    [Required(ErrorMessage = "Informe o aluno")]
    public int StudentId { get; set; }
}
