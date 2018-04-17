# Montapacking Blockchain
This blockchain prototype is built to investigate the possibilities of blockchain for the high-frequency supplychain network. This blockchain prototype specializes in b2c/b2b online orders, flowing through the supplychain. The webshop creates an SKU and is able to create an unlimited amount of supply for that SKU. Then the webshop can send supply to other parties in the entire supplychain. Only parties who own supply can transfer it to another; the webshop stays in control of the SKU data and total supply.

## Actions
This prototype is capable of handling different 'actions' in transactions to register supplychain activities:
- CreateSku
- ChangeSku (will update all fields except initial supply)
- CreateSupply (for an existing SKU)
- TransferSupply
- DestroySupply (this can happen when the product reaches the end of the supplychain: The recipient of the order)
- TransferToken

## Fees
Every action costs a certain amount of fee (in tokens):
- CreateSku: 100TK
- ChangeSku: 100TK
- CreateSupply: 100TK
- TransferSupply: 1TK
- DestroySupply: 1TK
- TransferToken: 10TK

## Specifications
The blockchain protocol is heavily based on bitcoin, but is built from scratch in C# with dotnet core 2.0. The blockchain network must handle atleast 200,000 transactions every day.
- Consensus algorithm: Proof-of-Work
- Block time: 15 sec
- Difficulty readjust: every 10 blocks
- Block mining reward: fixed 5000 tokens
- Supporting platforms: MacOS, Linux x86 and Windows x86

### Supporting platforms
- win10-x64
- osx.10.10-x64
- ubuntu.16.10-x64

## Installation
Download the repository files to your local file system and open up a command prompt in the `src/` folder to execute the following commands:
```sh
dotnet restore
dotnet build -c release
dotnet publish -c release -r {supporting-platform}
```

Now navigate to `src/Mpb.Node/bin/Release/netcoreapp2.0/{supporting-platform}` and there you will find the executable, named `Mpb.Node`. Run the executable.