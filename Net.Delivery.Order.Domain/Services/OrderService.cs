using Confluent.Kafka;
using System.Text.Json;
using System;
using System.Threading.Tasks;
using Net.Delivery.Order.Domain.Infrastructure.Repositories;
using Net.Delivery.Order.Domain.Entities;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Net.Delivery.Order.Domain.Services;

/// <summary>
/// Order service
/// </summary>
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly string _orderTopicName;
    private readonly string _kafkaBootstrapServers;

    /// <summary>
    /// Order service builder
    /// </summary>
    /// <param name="orderRepository">Order repository</param>
    /// <param name="configuration">Global configuration (It is being used the "appsettings.json" file located in the solution's root</param>
    public OrderService(IConfiguration configuration, IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
        var configuration1 = configuration;

        _orderTopicName = configuration1["OrderSettings:OrderTopicName"];
        _kafkaBootstrapServers = configuration1["OrderSettings:KafkaBootstrapServer"];
    }

    /// <summary>
    /// Creates an order
    /// </summary>
    /// <param name="items">Order items</param>
    /// <param name="customer">Order customer</param>
    public async Task CreateOrder(IList<string> items, Customer customer)
    {
        var order = new Entities.Order(items, customer);

        _orderRepository.Add(order);

        await PublishMessageToTopic(order);
    }

    /// <summary>
    /// Updates an order
    /// </summary>
    /// <param name="orderId">Order identification</param>
    /// <param name="orderSituation">Order situation</param>
    public async Task UpdateOrderSituation(string orderId, OrderSituation orderSituation)
    {
        var orderToBeUpdated = _orderRepository.GetOrderById(orderId);

        orderToBeUpdated.OrderSituation = orderSituation;
        orderToBeUpdated.OrderLastUpdate = DateTime.Now;

        _orderRepository.Update(orderToBeUpdated);

        await PublishMessageToTopic(orderToBeUpdated);
    }

    /// <summary>
    /// Gets all orders to delivery
    /// </summary>
    public IList<Entities.Order> GetAllOrdersToDelivery()
        => _orderRepository.GetOrdersBySituation(OrderSituation.Created);

    /// <summary>
    /// Publishes message with order data to Kafka topic
    /// </summary>
    /// <param name="order">Order data</param>
    /// <returns></returns>
    private async Task PublishMessageToTopic(Entities.Order order)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = _kafkaBootstrapServers
        };

        var orderConvertedToJson = JsonSerializer.Serialize(order);

        using (var producer = new ProducerBuilder<Null, string>(config).Build())
        {
            await producer.ProduceAsync(_orderTopicName, new Message<Null, string> { Value = orderConvertedToJson });
        }
    }
}