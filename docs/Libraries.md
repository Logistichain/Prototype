# Libraries
This project solution contains six different libraries / projects:
- [Mpb.Consensus](../src/Mpb.Consensus/). This library is responsible for all of the blockchain logic, like the [creation of blocks](CreatingBlocks.md), [creation of transactions](CreatingTransactions.md), [block validations](BlockValidation.md) and [transaction validations](TxValidation.md) but also [signing and hashing](Cryptography.md).
- [Mpb.DAL](../src/Mpb.DAL/). The Data Access Layer is responsible for the local storage and retrieval of data, like the blocks and transactions.
- [Mpb.Model](../src/Mpb.Model/). This is the place where all [domain models](Models.md) are described, like Block, Transaction and SKU (Stock Keeping Unit, used for storing product data in the blockchain).
- [Mpb.Networking](../src/Mpb.Networking/). Handles all of the incoming and outgoing [peer-to-peer networking](Networking.md) communications.
- [Mpb.Shared](../src/Mpb.Shared/). Contains classes which are used across all libraries, but are not domain models. You can find constants and datastructures here.

There are also some test assemblies. Test assemblies mainly focus on covering key aspects of the blockchain, like block and transaction validations. Not every code is backed by unittests, read the [design rationale](Rationale.md) for more info.