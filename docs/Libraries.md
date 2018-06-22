# Libraries
This project solution contains six different libraries / projects:
- [Logistichain.Consensus](../src/Logistichain.Consensus/). This library is responsible for all of the blockchain logic, like the [creation of blocks](CreatingBlocks.md), [creation of transactions](CreatingTransactions.md), [block validations](BlockValidation.md) and [transaction validations](TxValidation.md) but also [signing and hashing](Cryptography.md).
- [Logistichain.DAL](../src/Logistichain.DAL/). The Data Access Layer is responsible for the local storage and retrieval of data, like the blocks and transactions.
- [Logistichain.Model](../src/Logistichain.Model/). This is the place where all [domain models](Models.md) are described, like Block, Transaction and SKU (Stock Keeping Unit, used for storing product data in the blockchain).
- [Logistichain.Networking](../src/Logistichain.Networking/). Handles all of the incoming and outgoing [peer-to-peer networking](Networking.md) communications.
- [Logistichain.Shared](../src/Logistichain.Shared/). Contains classes which are used across all libraries, but are not domain models. You can find constants, events and datastructures here.

There are also some test assemblies. Test assemblies mainly focus on covering key aspects of the blockchain, like block and transaction validations. Not every code is backed by unittests, read the [design rationale](Rationale.md) for more info.