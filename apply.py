#!/usr/bin/env python3
from winreg import *
from win32com.shell import shell, shellcon
from subprocess import Popen, run, DEVNULL
import cson, glob, os, re, time, traceback, ctypes, sys

KEY_FORMAT = 'custom_%s'

def delete_key_recursive(hive, path):
	""" Adapted from http://stackoverflow.com/a/40183836 """
	try:
		open_key = OpenKey(hive, path, access=KEY_ALL_ACCESS)
	except FileNotFoundError:
		return
	except PermissionError:
		# (ノ ゜Д゜)ノ ︵ ┻━┻
		if hive == HKEY_CLASSES_ROOT:
			fullpath = 'HKEY_CLASSES_ROOT\\' + path
		elif hive == HKEY_CURRENT_USER:
			fullpath = 'HKEY_CURRENT_USER\\' + path
		else:
			raise
		if run([ 'util/SetACL.exe', '-on', fullpath, '-ot', 'reg', '-actn', 'clear', '-clr', 'dacl' ], stdout=DEVNULL).returncode == 0:
			open_key = OpenKey(hive, path, access=KEY_ALL_ACCESS)
		else:
			raise
	for x in range(0, QueryInfoKey(open_key)[0]):
		delete_key_recursive(hive, rf'{path}\{EnumKey(open_key, 0)}')
	CloseKey(open_key)
	DeleteKey(hive, path)

def annihilate_association(ext):
	delete_key_recursive(HKEY_CLASSES_ROOT, '.' + ext)
	delete_key_recursive(HKEY_CLASSES_ROOT, r'SystemFileAssociations\.' + ext)
	delete_key_recursive(HKEY_CURRENT_USER, r'Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.' + ext)
	delete_key_recursive(HKEY_CURRENT_USER, r'Software\Microsoft\Windows\Roaming\OpenWith\FileExts\.' + ext)
	delete_key_recursive(HKEY_CLASSES_ROOT, KEY_FORMAT % ext)

def create_file_type_key(ext, name, icon, commands):
	key = CreateKey(HKEY_CLASSES_ROOT, KEY_FORMAT % ext)
	SetValueEx(key, '', 0, REG_SZ, name)
	icon_key = CreateKey(key, 'DefaultIcon')
	SetValueEx(icon_key, '', 0, REG_SZ, icon)
	CloseKey(icon_key)
	shell_key = CreateKey(key, 'shell')
	for cmd_key_name, (cmd_name, cmd) in commands.items():
		cmd_key = CreateKey(shell_key, cmd_key_name)
		if cmd_name is not None:
			SetValueEx(cmd_key, '', 0, REG_SZ, cmd_name)
		cmd_command_key = CreateKey(cmd_key, 'command')
		SetValueEx(cmd_command_key, '', 0, REG_SZ, cmd)
		CloseKey(cmd_command_key)
		CloseKey(cmd_key)
	CloseKey(shell_key)
	CloseKey(key)

def associate_extension(ext, as_ext=None, shell_new=False):
	key = CreateKey(HKEY_CLASSES_ROOT, '.' + ext)
	SetValueEx(key, '', 0, REG_SZ, KEY_FORMAT % (as_ext or ext))
	if shell_new:
		shell_new_key = CreateKey(key, 'ShellNew')
		SetValueEx(shell_new_key, 'NullFile', 0, REG_SZ, '')
		CloseKey(shell_new_key)
	CloseKey(key)

def main():

	# Ensure admin (assumes elevate.exe in PATH)

	if not ctypes.windll.shell32.IsUserAnAdmin():
		Popen([ 'elevate', 'py', os.path.realpath(__file__) ])
		sys.exit()

	os.chdir(os.path.dirname(__file__))

	# Load config file

	with open('config.cson', 'rb') as f:
		config = cson.load(f)

	# Create directory for test files

	os.makedirs('test', exist_ok=True)

	# Loop over types

	for ext, filetype in config['types'].items():

		print(f'Applying {ext}...')

		# Nuke existing association from orbit

		if filetype.get('nuke', True):
			annihilate_association(ext)
			if 'aliases' in filetype:
				for alias in filetype['aliases']:
					annihilate_association(alias)

		# Resolve icon

		if 'icon' in filetype and '\\' in filetype['icon']:
			icon = filetype['icon']
		else:
			icon_name = filetype.get('icon', ext)
			icons = glob.glob('icons/' + glob.escape(icon_name) + '.ico') + \
			        glob.glob('icons/' + glob.escape(icon_name) + '.*.ico')
			if len(icons) == 0:
				raise Exception(f'Icon "{icon_name}" not found')
			elif len(icons) > 1:
				raise Exception(f'More than one "{icon_name}" icon exists')
			icon = os.path.abspath(icons[0])

		# Resolve commands

		commands = {
			'open': (None, config['commands'].get(filetype.get('open', 'default'), filetype.get('open')))
		}

		if 'menu' in filetype:
			for name, command in filetype['menu'].items():
				key = re.sub(r'\W', '', name).lower()
				commands[key] = name, config['commands'].get(command, command)

		# Create association

		create_file_type_key(ext, filetype['name'], icon, commands)
		associate_extension(ext, shell_new=filetype.get('shellNew', False))
		if 'aliases' in filetype:
			for alias in filetype['aliases']:
				associate_extension(alias, ext)

		# Create test file

		open('test/test.' + ext, 'a').close()

	# Trigger shell update

	shell.SHChangeNotify(shellcon.SHCNE_ASSOCCHANGED, shellcon.SHCNF_IDLIST, None, None)

	# Whoop

	print('Done.')
	sys.stdout.flush()
	time.sleep(1)


if __name__ == '__main__':
	try:
		main()
	except Exception:
		traceback.print_exc()
		input('\nPress enter to close...')
