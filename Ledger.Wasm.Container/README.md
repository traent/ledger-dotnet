# Ledger Wasm Container

## Run
To directly compile and serve the content, run the following command:
```
sh start.sh
```

## Manual Compilation
- Run the following to produce the WASM transpiled code:
    ```
    dotnet publish -c Release
    ```
- The output can be found inside the `./bin/Release/net6.0/publish/wwwroot` folder