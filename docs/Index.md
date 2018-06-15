# Logistichain documentation
Hello there, adventurer! Welcome to the world of blockchain. This prototype is meant to generate knowledge about blockchain. That's why this blockchain was created from scratch in C#. The project also investigates if a high-volume supplychain (at least 200K transactions a day) blockchain is feasible or not. To help you understand this project and it's internals, feel free to open up some subjects from the documentation guide. Most interfaces and classes are provided with documentation aswell, so another option is to skip the docs and take a deep-dive into the code.

## Documentation guide
- Explore our components by yourself:
    * [Libraries.md](Libraries.md): Explanation of the solution architecture
    * [Models.md](Models.md): Block, Transaction, SKU. (Models library)
    * [Mining.md](Mining.md): Mining & Difficulty. (Node library)
    * [CreatingBlocks.md](CreatingBlocks.md): Creating blocks and adding transactions. (Consensus library)
    * [Cryptography.md](Cryptography.md): Signing, hashing, public and private keys. (Consensus library)
    * [BlockValidation.md](BlockValidation.md): Block validation. (Consensus library)
    * [TxValidation.md](TxValidation.md): Transaction validation. (Consensus library)
    * [Networking.md](Networking.md): Peer-to-peer network connections, RPC. (Networking library)
- [Architecture.md](Architecture.md): Get the bigger picture and see how the software was designed.
- [Rationale.md](Rationale.md): Read about the design decisions.
- [CodeConventions.md](CodeConventions.md): See what kind of code conventions are used.
- [Testing.md](Testing.md): Describes the policy about writing unit tests.