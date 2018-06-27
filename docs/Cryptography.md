# Cryptography
Cryptography is essential to a blockchain. Cryptography guarantees the integrity of the data and makes data tamper-proof.
A transaction is made tamper-proof by calculating a SHA-256 hash of all the transaction's properties. This way, when the data is altered afterwards, the calculated hash is different - so alterations can be noticed.
This goes the same for blocks. After the transactions are injected, the block is sealed by calculating and saving the SHA-256 hash. This means the transactions cannot be removed or reordered.

The asymmetric algorithm for generating signatures, public and private keys is the same as bitcoin's: [secp256k1](https://en.bitcoin.it/wiki/Secp256k1).
We'll be using [BouncyCastle](https://www.bouncycastle.org/) to implement this cryptography.
The transaction as well as the block will be signed by using a private key.

# Signing a block
When a miner creates a valid block, he creates a ClaimCoinbase transaction and puts his public key in the `to` field - effectively claiming new tokens. The miner signs the transaction and signs the block with his private key.
The consensus algorithm will use this `to` field from the ClaimCoinbase transaction to verify the block signature.