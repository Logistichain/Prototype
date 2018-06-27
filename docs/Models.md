# Models
The [Logistichain.Model](../src/Logistichain.Model/) library contains all domain models for this solution. Keep in mind that the domain is focussed on researching blockchain for a supplychain environment, so these two subjects are both within the domain.

## 1. Transaction
The first model is a blockchain transaction. The transaction contains information about the sender, receiver and the data that is exchanged between these two parties. The [architecture design](Architecture.md) states that we have an [AbstractTransaction](../src/Logistichain.Model/AbstractTransaction.cs) and [StateTransaction](../src/Logistichain.Model/StateTransaction.cs) model. AbstractTransaction is a blueprint for transaction implementations. This layer of abstraction enhances decoupling, testability and allows different kinds of transaction structures to exist, each having a different version ofcourse.

### 1.1 State vs UTXO
This project implements the state-transition transaction model, inspired by Ethereum. The [design rationale](Rationale.md) explains why we chose for this setup.
