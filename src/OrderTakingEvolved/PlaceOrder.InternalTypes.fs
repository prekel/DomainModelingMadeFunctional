module internal OrderTaking.PlaceOrder.InternalTypes

open OrderTaking.Common

// ======================================================
// Define each step in the PlaceOrder workflow using internal types
// (not exposed outside the bounded context)
// ======================================================

// ---------------------------
// Validation step
// ---------------------------

// Product validation

type CheckProductCodeExists = ProductCode -> bool

// Address validation
type AddressValidationError =
    | InvalidFormat
    | AddressNotFound

type CheckedAddress = CheckedAddress of UnvalidatedAddress

type CheckAddressExists = UnvalidatedAddress -> AsyncResult<CheckedAddress, AddressValidationError>

// ---------------------------
// Validated Order
// ---------------------------

type PricingMethod =
    | Standard
    | Promotion of PromotionCode

type ValidatedOrderLine =
    { OrderLineId: OrderLineId
      ProductCode: ProductCode
      Quantity: OrderQuantity }

type ValidatedOrder =
    { OrderId: OrderId
      CustomerInfo: CustomerInfo
      ShippingAddress: Address
      BillingAddress: Address
      Lines: ValidatedOrderLine list
      PricingMethod: PricingMethod }

type ValidateOrder =
    CheckProductCodeExists -> CheckAddressExists -> UnvalidatedOrder -> AsyncResult<ValidatedOrder, ValidationError> // input

// ---------------------------
// Pricing step
// ---------------------------


type GetProductPrice = ProductCode -> Price

type TryGetProductPrice = ProductCode -> Price option

type GetPricingFunction = PricingMethod -> GetProductPrice

type GetStandardPrices =
    // no input -> return standard prices
    unit -> GetProductPrice

type GetPromotionPrices =
    // promo input -> return prices for promo, maybe
    PromotionCode -> TryGetProductPrice




// priced state
type PricedOrderProductLine =
    { OrderLineId: OrderLineId
      ProductCode: ProductCode
      Quantity: OrderQuantity
      LinePrice: Price }

type PricedOrderLine =
    | ProductLine of PricedOrderProductLine
    | CommentLine of string

type PricedOrder =
    { OrderId: OrderId
      CustomerInfo: CustomerInfo
      ShippingAddress: Address
      BillingAddress: Address
      AmountToBill: BillingAmount
      Lines: PricedOrderLine list
      PricingMethod: PricingMethod }

type PriceOrder =
    GetPricingFunction -> ValidatedOrder -> Result<PricedOrder, PricingError> // input

// ---------------------------
// Shipping
// ---------------------------

type ShippingMethod =
    | PostalService
    | Fedex24
    | Fedex48
    | Ups48

type ShippingInfo =
    { ShippingMethod: ShippingMethod
      ShippingCost: Price }

type PricedOrderWithShippingMethod =
    { ShippingInfo: ShippingInfo
      PricedOrder: PricedOrder }

type CalculateShippingCost = PricedOrder -> Price

type AddShippingInfoToOrder =
    CalculateShippingCost -> PricedOrder -> PricedOrderWithShippingMethod // input

// ---------------------------
// VIP shipping
// ---------------------------

type FreeVipShipping = PricedOrderWithShippingMethod -> PricedOrderWithShippingMethod

// ---------------------------
// Send OrderAcknowledgment
// ---------------------------

type HtmlString = HtmlString of string

type OrderAcknowledgment =
    { EmailAddress: EmailAddress
      Letter: HtmlString }

type CreateOrderAcknowledgmentLetter = PricedOrderWithShippingMethod -> HtmlString

/// Send the order acknowledgement to the customer
/// Note that this does NOT generate an Result-type error (at least not in this workflow)
/// because on failure we will continue anyway.
/// On success, we will generate a OrderAcknowledgmentSent event,
/// but on failure we won't.

type SendResult =
    | Sent
    | NotSent

type SendOrderAcknowledgment = OrderAcknowledgment -> SendResult

type AcknowledgeOrder =
    CreateOrderAcknowledgmentLetter -> SendOrderAcknowledgment -> PricedOrderWithShippingMethod -> OrderAcknowledgmentSent option // output

// ---------------------------
// Create events
// ---------------------------

type CreateEvents =
    PricedOrder -> OrderAcknowledgmentSent option -> PlaceOrderEvent list // output
