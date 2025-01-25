# Solana Payment Service (SolPaymentService)

This project provides a **Solana payment service** implemented in C#. It allows you to manage Solana wallets using **Hierarchical Deterministic (HD)** wallets and perform transactions like checking balances, recording payments, transferring funds, and managing a pool of wallets.

### Features:
- **HD Wallet Generation**: Create Solana wallets from a master seed.
- **Transaction Tracking**: Track payments made to specific wallets and transfer funds.
- **Wallet Pool Management**: Manage a pool of Solana addresses, with the ability to create new ones when needed.
- **Automatic Payment Checking**: A periodic checker cycle checks for balances and triggers transactions.
- **Configurable Parameters**: Set wallet expiration times and other settings.

---

## TODO
- Config object for payment service
- Exception system

## Support
If you liked it, please buy me a coffee
Sol: C57XeiA2fru7MoHsa71QTYxZNFrdHBdxjSNSHbrgBWsc

## Installation

### Prerequisites
- .NET 6 or higher
- Solana RPC endpoint (use a public one or set up your own)
- Git for cloning the repository



### Steps to Install
1. **Clone the repository**:

   ```bash
   git clone https://github.com/your-username/SolanaPaymentHD.git
   cd SolanaPaymentHD
