tuyapi/cli [![Build Status](https://travis-ci.org/TuyaAPI/cli.svg?branch=master)](https://travis-ci.org/TuyaAPI/cli) [![XO code style](https://img.shields.io/badge/code_style-XO-5ed9c7.svg)](https://github.com/xojs/xo)
===========================

A CLI for Tuya devices.

## Installation
`npm i @tuyapi/cli -g`

## Usage

```shell
> tuya-cli help

  Usage: tuya-cli [options] [command]

  Options:
    -V, --version       output the version number
    -h, --help          display help for command

  Commands:
    cloud [options]
    link [options]      link a new device
    get [options]       get a property on a device
    set [options]       set a property on a device
    list                list all locally saved devices
    list-app [options]  list devices from Tuya Smart app (deprecated)
    mock [options]      mock a Tuya device for local testing
    wizard [options]    list devices from an offical app
    help                output usage information
```

## Development
1. After cloning, run `npm i`.
2. To run tests, run `npm test`.
