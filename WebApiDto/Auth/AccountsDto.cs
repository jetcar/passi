using System.Collections.Generic;

namespace WebApiDto.Auth
{
    public class AccountsDto
    {
        public List<AccountMinDto> Accounts { get; set; } = new List<AccountMinDto>();
    }
}