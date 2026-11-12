using Application.DTOs;
using Domain.Entities;

namespace Application.IServices
{
    public interface ITransferService
    {
        Task<List<TransferItem>> GetPendingItemsAsync();
        Task<TransferResult> SendTransferAsync(TransferRequest request);
        Task<TransferResult> ConfirmReceiptAsync(int transferItemId, decimal quantityReceived);
    }
}