///
/// \file SoapySDR/Modules.hpp
///
/// Utility functions to deal with modules.
/// These utility functions are made available for advanced usage.
/// For most use cases, the API will automatically load modules.
///
/// \copyright
/// Copyright (c) 2014-2020 Josh Blum
/// SPDX-License-Identifier: BSL-1.0
///

#pragma once
#include <SoapySDR/Config.hpp>
#include <SoapySDR/Types.hpp>
#include <vector>
#include <string>

namespace SoapySDR
{
	//! Query the root installation path
	SOAPY_SDR_API std::string getRootPath(void);

	/*!
	 * The list of paths automatically searched by loadModules().
	 * \return a list of automatically searched file paths
	 */
	SOAPY_SDR_API std::vector<std::string> listSearchPaths(void);

	/*!
	 * List all modules found in default path.
	 * \return a list of file paths to loadable modules
	 */
	SOAPY_SDR_API std::vector<std::string> listModules(void);

	/*!
	 * List all modules found in the given path.
	 * \param path a directory on the system
	 * \return a list of file paths to loadable modules
	 */
	SOAPY_SDR_API std::vector<std::string> listModules(const std::string& path);

	/*!
	 * Load a single module given its file system path.
	 * \param path the path to a specific module file
	 * \return an error message, empty on success
	 */
	SOAPY_SDR_API std::string loadModule(const std::string& path);

	/*!
	 * List all registration loader errors for a given module path.
	 * The resulting dictionary contains all registry entry names
	 * provided by the specified module. The value of each entry
	 * is an error message string or empty on successful load.
	 * \param path the path to a specific module file
	 * \return a dictionary of registry names to error messages
	 */
	SOAPY_SDR_API Kwargs getLoaderResult(const std::string& path);

	/*!
	 * Get a version string for the specified module.
	 * Modules may optionally provide version strings.
	 * \param path the path to a specific module file
	 * \return a version string or empty if no version provided
	 */
	SOAPY_SDR_API std::string getModuleVersion(const std::string& path);

	/*!
	 * Unload a module that was loaded with loadModule().
	 * \param path the path to a specific module file
	 * \return an error message, empty on success
	 */
	SOAPY_SDR_API std::string unloadModule(const std::string& path);

	/*!
	 * Load the support modules installed on this system.
	 * This call will only actually perform the load once.
	 * Subsequent calls are a NOP.
	 */
	SOAPY_SDR_API void loadModules(void);

	/*!
	 * Unload all currently loaded support modules.
	 */
	SOAPY_SDR_API void unloadModules(void);

	/*!
	 * Manage the lifetime of loadable modules
	 * by unloading modules on scope exit.
	 */
	class SOAPY_SDR_API ModuleManager
	{
	public:
		/*!
		 * Create an instance of the module manager.
		 * Loading modules on creation can be disabled.
		 * \param load false to skip loading modules
		 */
		ModuleManager(const bool load = true);

		/*!
		 * Module manager destructor.
		 * Unload modules on cleanup.
		 */
		~ModuleManager(void);
	};

	//! \cond
	//! Internal call to register version with a module during load
	class SOAPY_SDR_API ModuleVersion
	{
	public:
		ModuleVersion(const std::string& version);
	};
	//! \endcond
}