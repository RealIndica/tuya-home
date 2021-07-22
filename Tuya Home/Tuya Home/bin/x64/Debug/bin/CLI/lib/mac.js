const {TuyaContext} = require('@tuya/tuya-connector-nodejs');
const inquirer = require('inquirer');
const colors = require('colors');
const any = require('promise.any');
const AggregateError = require('es-aggregate-error/polyfill')();
const {regionToUrl} = require('./helpers');

const REGIONS = ['eu', 'us', 'cn', 'in'];

const list = async (conf, options) => {
	
	const savedAPIRegion = conf.get('apiRegion')
	
	// Get seed device
	let userId;
	let foundAPIRegion = savedAPIRegion;

	try {
		const {device, region} = await any((savedAPIRegion ? [savedAPIRegion] : REGIONS).map(async region => {
			const api = new TuyaContext({
				baseUrl: regionToUrl(region),
				accessKey: options.apiKey,
				secretKey: options.apiSecret
			});

			const result = await api.request({
				method: 'GET',
				path: `/v1.0/devices/${options.virtualKey}`
			});

			if (!result.success) {
				throw new Error(`${result.code}: ${result.msg}`);
			}

			return {device: result.result, region};
		}));

		userId = device.uid;
		foundAPIRegion = region;
	} catch (error) {
		if (process.env.DEBUG) {
			if (error.constructor === AggregateError) {
				console.error(error.errors);
			} else {
				console.error(error);
			}
		}

		console.error(colors.red('There was an issue fetching that device. Make sure your account is linked and the ID is correct.'));

		// eslint-disable-next-line unicorn/no-process-exit
		process.exit(1);
	}

	// Get user devices
	const api = new TuyaContext({
		baseUrl: regionToUrl(foundAPIRegion),
		accessKey: options.apiKey,
		secretKey: options.apiSecret
	});

	const result = await api.request({
		method: 'GET',
		path: `/v1.0/devices/factory-infos?device_ids=${options.virtualKey}`
	});

	if (!result.success) {
		throw new Error(`${result.code}: ${result.msg}`);
	}


	console.log(result.result);
};

module.exports = list;
