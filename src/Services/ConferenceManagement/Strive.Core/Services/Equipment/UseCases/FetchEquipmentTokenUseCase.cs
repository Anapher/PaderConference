using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Strive.Core.Interfaces.Services;
using Strive.Core.Services.Equipment.Gateways;
using Strive.Core.Services.Equipment.Requests;

namespace Strive.Core.Services.Equipment.UseCases
{
    public class FetchEquipmentTokenUseCase : IRequestHandler<FetchEquipmentTokenRequest, string>
    {
        private readonly ITokenFactory _tokenFactory;
        private readonly IEquipmentTokenRepository _equipmentTokenRepository;

        public FetchEquipmentTokenUseCase(ITokenFactory tokenFactory,
            IEquipmentTokenRepository equipmentTokenRepository)
        {
            _tokenFactory = tokenFactory;
            _equipmentTokenRepository = equipmentTokenRepository;
        }

        public async Task<string> Handle(FetchEquipmentTokenRequest request, CancellationToken cancellationToken)
        {
            var token = _tokenFactory.GenerateToken();
            await _equipmentTokenRepository.Set(request.Participant, token);
            return token;
        }
    }
}
