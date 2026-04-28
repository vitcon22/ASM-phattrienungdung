using FruitShop.Models.Entities;
using FruitShop.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace FruitShop.Services;

/// <summary>
/// Service quản lý giỏ hàng - tách khỏi OrderController
/// </summary>
public interface ICartService
{
    List<CartItemViewModel> GetCart(ISession session);
    void SaveCart(ISession session, List<CartItemViewModel> cart);
    void AddToCart(ISession session, Fruit fruit, int quantity);
    void UpdateQuantity(ISession session, int fruitId, int quantity);
    void RemoveFromCart(ISession session, int fruitId);
    void ClearCart(ISession session);
    int GetCartCount(ISession session);
    decimal GetCartTotal(ISession session);
}

public class CartService : ICartService
{
    private const string CartKey = "Cart";

    public List<CartItemViewModel> GetCart(ISession session)
    {
        var json = session.GetString(CartKey);
        return string.IsNullOrEmpty(json)
            ? new List<CartItemViewModel>()
            : JsonSerializer.Deserialize<List<CartItemViewModel>>(json)
              ?? new List<CartItemViewModel>();
    }

    public void SaveCart(ISession session, List<CartItemViewModel> cart)
    {
        session.SetString(CartKey, JsonSerializer.Serialize(cart));
    }

    public void AddToCart(ISession session, Fruit fruit, int quantity)
    {
        var cart = GetCart(session);
        var existing = cart.FirstOrDefault(x => x.FruitId == fruit.FruitId);

        if (existing != null)
        {
            int newQty = Math.Min(existing.Quantity + quantity, fruit.StockQuantity);
            existing.Quantity = newQty;
            existing.StockQuantity = fruit.StockQuantity;
        }
        else
        {
            cart.Add(new CartItemViewModel
            {
                FruitId       = fruit.FruitId,
                FruitName    = fruit.FruitName,
                UnitPrice    = fruit.Price,
                Quantity     = Math.Min(quantity, fruit.StockQuantity),
                ImageUrl     = fruit.GetImageUrl(),
                Unit         = fruit.Unit,
                StockQuantity = fruit.StockQuantity
            });
        }
        SaveCart(session, cart);
    }

    public void UpdateQuantity(ISession session, int fruitId, int quantity)
    {
        var cart = GetCart(session);
        var item = cart.FirstOrDefault(x => x.FruitId == fruitId);
        if (item == null) return;

        if (quantity <= 0)
            cart.Remove(item);
        else
            item.Quantity = Math.Min(quantity, item.StockQuantity);

        SaveCart(session, cart);
    }

    public void RemoveFromCart(ISession session, int fruitId)
    {
        var cart = GetCart(session);
        cart.RemoveAll(x => x.FruitId == fruitId);
        SaveCart(session, cart);
    }

    public void ClearCart(ISession session)
    {
        session.Remove(CartKey);
    }

    public int GetCartCount(ISession session)
    {
        return GetCart(session).Sum(x => x.Quantity);
    }

    public decimal GetCartTotal(ISession session)
    {
        return GetCart(session).Sum(x => x.Subtotal);
    }
}
