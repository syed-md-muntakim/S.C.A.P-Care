using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartClinic.Web.Common;
using SmartClinic.Web.Models;
using SmartClinic.Web.Services;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class MedicineInventoryController : Controller
{
    private readonly IMedicineInventoryService _inventoryService;

    public MedicineInventoryController(IMedicineInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async Task<IActionResult> Index(
        string? searchTerm,
        string? statusFilter = "all")
    {
        var model = await _inventoryService.GetInventoryAsync(searchTerm, statusFilter);
        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new MedicineFormViewModel
        {
            ExpiryDate = DateTime.Today.AddYears(1),
            ReorderLevel = 10,
            Unit = "Tablet"
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MedicineFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _inventoryService.CreateAsync(model, User.Identity?.Name ?? "Admin");
        TempData["Success"] = "Medicine was added to inventory.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _inventoryService.GetFormAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MedicineFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var updateResult = await _inventoryService.UpdateAsync(id, model);
        if (updateResult == MedicineWriteResult.NotFound)
        {
            return NotFound();
        }

        if (updateResult == MedicineWriteResult.ConcurrencyConflict)
        {
            TempData["Error"] = "This medicine was updated by another user. Reload the form and try again.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        TempData["Success"] = "Medicine details were updated. Stock quantity was not changed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _inventoryService.GetDetailsAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AdjustStock(int id)
    {
        var medicine = await _inventoryService.GetFormAsync(id);
        if (medicine is null)
        {
            return NotFound();
        }

        return View(new MedicineStockAdjustmentViewModel
        {
            MedicineId = medicine.Id,
            MedicineName = medicine.Name,
            CurrentQuantity = medicine.QuantityInStock
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(MedicineStockAdjustmentViewModel model)
    {
        var medicine = await _inventoryService.GetFormAsync(model.MedicineId);
        if (medicine is null)
        {
            return NotFound();
        }

        model.MedicineName = medicine.Name;
        model.CurrentQuantity = medicine.QuantityInStock;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _inventoryService.AdjustStockAsync(
            model,
            User.Identity?.Name ?? "Admin");

        switch (result)
        {
            case StockAdjustmentResult.Success:
                TempData["Success"] = "Stock quantity was updated and the movement was recorded.";
                return RedirectToAction(nameof(Details), new { id = model.MedicineId });

            case StockAdjustmentResult.NotFound:
                return NotFound();

            case StockAdjustmentResult.InsufficientStock:
                ModelState.AddModelError(
                    nameof(model.Quantity),
                    "Stock-out quantity cannot be greater than the available quantity.");
                break;

            case StockAdjustmentResult.QuantityOverflow:
                ModelState.AddModelError(
                    nameof(model.Quantity),
                    "The resulting stock quantity is too large.");
                break;

            case StockAdjustmentResult.InvalidMovementType:
                ModelState.AddModelError(
                    nameof(model.MovementType),
                    "Select a valid stock movement type.");
                break;

            case StockAdjustmentResult.ConcurrencyConflict:
                TempData["Error"] = "This medicine's stock changed while you were editing it. Review the refreshed quantity and try again.";
                return RedirectToAction(nameof(AdjustStock), new { id = model.MedicineId });
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var deleteResult = await _inventoryService.DeleteAsync(id);
        if (deleteResult == MedicineWriteResult.NotFound)
        {
            return NotFound();
        }

        if (deleteResult == MedicineWriteResult.ConcurrencyConflict)
        {
            TempData["Error"] = "This medicine was changed by another user and was not removed. Review the inventory and try again.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Medicine was removed from the active inventory. Its stock history was retained.";
        return RedirectToAction(nameof(Index));
    }
}
