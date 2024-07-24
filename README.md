# Digital Wallet System

## Overview

The Digital Wallet System is a secure and efficient platform that allows users to manage their funds. Users can store money, transfer funds, and track their spending, all while ensuring the highest level of security with features like encryption. The system is built using modern technologies to provide a robust and scalable solution.

## Features

- **User Authentication and Authorization**: Secure login and registration using JWT.
- **Wallet Management**: Create and manage digital wallets for users.
- **Funds Transfer**: Transfer funds between wallets securely.
- **Transaction History**: Track all transactions with detailed records.
- **Encryption**: Enhance security with data encryption.
- **Integration with Paystack**: Secure payment gateway integration for deposits.
- **Integration with Redis**: Idempotency key setup to prevent duplicate wallet transfers.

## Technology Stack

- **Backend**: C#, ASP.NET Core, Entity Framework Core,
- **Database**: SQL Server
- **Authentication**: JWT
- **Payment Gateway**: Paystack API
- **Idempotency Keys**: Redis

## Prerequisites

- .NET 6.0 SDK or later
- SQL Server
- Redis
- Paystack Account (for payment integration)


## Database Design

Digital Wallet System uses a relational database with the following structure:

### Entities

| Entity | Attributes |
|--------|------------|
| User   | - Id (int, primary key)<br>- Username (string, unique)<br>- Email (string, unique)<br>- Password (string) |
| Wallet | - Id (int, primary key)<br>- Balance (decimal)<br>- UserId (int, foreign key to User) |
| DepositTransaction | - Id (int, primary key)<br>- UserId (int, foreign key to User)<br>- Amount (decimal)<br>- Timestamp (DateTime)<br>- Status (string)<br>- Reference (string)<br>- ReferenceHash (string)<br>- TransactionType (string) |
| TransferTransaction | - Id (int, primary key)<br>- SenderId (int, foreign key to User)<br>- RecipientId (int, foreign key to User)<br>- Amount (decimal)<br>- Timestamp (DateTime)<br>- TransactionType (string) |

### Relationships

| Relationship | Description |
|--------------|-------------|
| User <-> Wallet | One-to-One |
| User -> DepositTransaction | One-to-Many |
| User -> TransferTransaction (as Sender) | One-to-Many |
| User -> TransferTransaction (as Recipient) | One-to-Many |

### Constraints and Behaviors

| Type | Description |
|------|-------------|
| Unique Constraints | - Username in User table<br>- Email in User table |
| Foreign Key Constraints | - Wallet.UserId to User.Id<br>- DepositTransaction.UserId to User.Id<br>- TransferTransaction.SenderId to User.Id<br>- TransferTransaction.RecipientId to User.Id |
| Delete Behavior | Restrict delete on User for related TransferTransactions and DepositTransactions |


## Getting Started

### Clone the Repository

```bash
git clone https://github.com/NuelUzoma/digital-wallet-system.git
cd digital-wallet-system
```

### Configuration

#### Database Configuration

Update the connection string in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=your_server;Database=your_database;User Id=your_user;Password=your_password;"
}
```

#### JWT Configuration

Update the JWT settings in `appsettings.json`:

```json
"Jwt": {
  "Key": "your_secret_key",
  "Issuer": "your_issuer",
  "Audience": "your_audience"
}
```

#### Paystack Configuration

Add your Paystack API keys in `appsettings.json`:

```json
"Paystack": {
  "SecretKey": "your_paystack_secret_key"
}
```

#### Redis Configuration
```json
"Redis": {
    "Configuration": "localhost:6379"
},
```

### Migrations and Database Update

Apply migrations and update the database:

```bash
dotnet dotnet-ef migrations add InitialCreate
dotnet dotnet-ef database update
```

### Running the Application

```bash
dotnet run
```

The application will be available at `http://localhost:5052`.

## API Documentation

The API Documentation is available on [API Documentation](https://documenter.getpostman.com/view/27344999/2sA3kXELSP)

## Security Features

### Data Encryption

All sensitive data, including user passwords and transaction details, are encrypted using industry-standard encryption algorithms.

## Contribution

We welcome contributions! Please follow these steps to contribute:

1. Fork the repository.
2. Create a new branch (`git checkout -b feature-branch`).
3. Make your changes and commit them (`git commit -m 'Add new feature'`).
4. Push to the branch (`git push origin feature-branch`).
5. Create a new Pull Request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE.md) file for more details.

---