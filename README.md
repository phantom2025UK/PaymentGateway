# Payment Gateway Implementation

This project implements a payment gateway API that allows merchants to process payments and retrieve payment details. The implementation follows the requirements specified in the Checkout.com technical assessment.

## Implementation Notes

### Key Components

- **Payment Validation Service**: Performs comprehensive validation of payment requests
- **Bank Client**: Communicates with the acquiring bank (simulator)
- **Payment Service**: Orchestrates the payment processing flow
- **Payments Controller**: Exposes the API endpoints
- **Payments Repository**: In-memory storage for payment records

### Design Considerations and Assumptions

1. **Resilience**: Implemented Polly for transient error handling in the Bank Client, focusing only on transient errors (network issues, timeouts) with exponential backoff and retry strategy.

2. **Validation**: Comprehensive validation according to the specified requirements, with clear error messages for each validation rule.

3. **Security**: Only storing the last four digits of card numbers to maintain compliance.

4. **Status Mapping**:
   - `Authorized`: Payment authorized by the bank
   - `Declined`: Payment declined by the bank
   - `Rejected`: Invalid payment details detected before reaching the bank

5. **Error Handling**: Consistent error handling approach with appropriate HTTP status codes.

6. **Service Unavailability**: Handling bank service unavailability gracefully (for cards ending with 0).

7. **Configuration-Based Logic**: Using appsettings.json for configurable parameters like supported currencies and validation rules.

### Testing

- Unit tests implemented for the core components:
  - Comprehensive tests for validation logic
  - Tests for payment service logic using a real repository
  - Tests for bank client with mocked HTTP responses
  - Controller tests using WebApplicationFactory
- Tests cover different payment scenarios:
  - Successful payments (cards ending with odd digits)
  - Declined payments (cards ending with even digits)
  - Service unavailability (cards ending with zero)
  - Invalid input validation
- Note: True end-to-end integration tests with the bank simulator are not implemented

### Limitations and Potential Improvements

1. **Error Types**: Currently returning error messages as strings; could be improved by implementing structured error types for better client handling.

2. **Test Coverage**: While validation service has comprehensive coverage, Bank client and PaymentService tests have sufficient coverage to demonstrate testing approach but are not exhaustive.

3. **Integration Testing**: No true end-to-end integration tests with the bank simulator; these would be valuable to verify the complete payment flow.

4. **Persistence**: Using in-memory storage for simplicity;

5. **Authentication & Authorization**: Not implemented in this version but would be critical for a production system. Could be implemented either directly within the API using JWT tokens and policies, or by hosting the API behind a commercial API management gateway (like Azure API Management) to offload authentication, rate limiting, and other security concerns.

6. **Metrics & Monitoring**: Could add monitoring for payment processing performance and error rates.

7. **API Versioning**: No API versioning strategy implemented; Could be implemented using URL path versioning (e.g., /api/v1/payments), request headers (Api-Version: 1.0), or content negotiation with media types (application/vnd.checkout.v1+json). ASP.NET Core provides built-in support via Microsoft.AspNetCore.Mvc.Versioning package.

