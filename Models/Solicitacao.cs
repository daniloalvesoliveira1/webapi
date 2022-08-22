
namespace WebApi.Models;

public class Solicitacao  {    

    public Int64 idSolicitacao {get;set;}
    public DateTime dataSolicitacao {get;set;}
    public String descricaoSolicitacao{get;set;} ="";
    public Status status{get;set;} = new Status();
}