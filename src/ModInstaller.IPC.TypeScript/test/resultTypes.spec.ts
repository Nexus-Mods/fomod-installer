import { describe, test, expect } from 'vitest';

import { InstallResult, InstallSnapshot } from '../src/types';

/**
 * Pins the IPC transport's result shape.
 *
 * The IPC server serialises with Newtonsoft.Json and no naming policy, so the payload is
 * PascalCase; only the `optionSnapshot` key is lower-case, because it is a dictionary key
 * added in the Dynamic adaptor. These declarations previously claimed camelCase throughout,
 * which silently made every field undefined for a TypeScript consumer.
 *
 * The payload below is the real shape emitted by ModInstaller.IPC/Server.cs. Assigning it to
 * InstallResult is the actual assertion: tsc -p tsconfig.test.json fails if the declarations
 * and the wire format disagree.
 */
const wirePayload: InstallResult = {
  message: 'Installation successful',
  instructions: [],
  optionSnapshot: {
    Steps: [
      {
        Id: 0,
        Name: 'Step1',
        Visible: true,
        Groups: [
          {
            Name: 'PatchForModB',
            Type: 'SelectAny',
            Options: [
              {
                Name: 'Patch for Mod B',
                Type: 'NotUsable',
                Selected: false,
                Resolver: {
                  Kind: 'Conditional',
                  DefaultType: 'NotUsable',
                  Patterns: [
                    {
                      Type: 'Required',
                      Condition: {
                        Kind: 'Composite',
                        Operator: 'Or',
                        Conditions: [
                          { Kind: 'Plugin', PluginPath: 'b.esp', PluginState: 'Active' },
                          { Kind: 'Environment', EnvironmentType: 'GameVersion', MinimumVersion: '99.0.0' },
                        ],
                      },
                    },
                  ],
                },
                Flags: [{ Name: 'patches', Value: 'enabled' }],
              },
            ],
          },
        ],
      },
    ],
    Flags: [{ Name: 'patches', Value: 'enabled' }],
  },
};

describe('IPC result types', () => {
  test('snapshot payload is PascalCase beneath the lower-case optionSnapshot key', () => {
    const snapshot = wirePayload.optionSnapshot as InstallSnapshot;

    expect(snapshot).toBeDefined();
    expect(snapshot.Steps).toHaveLength(1);
    expect(snapshot.Steps[0].Groups[0].Options[0].Resolver.Kind).toBe('Conditional');
    expect(snapshot.Steps[0].Groups[0].Options[0].Resolver.Patterns?.[0].Type).toBe('Required');

    // The camelCase spellings the declarations used to claim must not exist on the wire.
    expect((snapshot as unknown as Record<string, unknown>).steps).toBeUndefined();
  });

  test('environment leaves carry a requirement but no observation', () => {
    const condition =
      wirePayload.optionSnapshot!.Steps[0].Groups[0].Options[0].Resolver.Patterns![0].Condition;

    const environment = condition.Conditions!.find((c) => c.Kind === 'Environment');
    expect(environment?.MinimumVersion).toBe('99.0.0');
    // No observed-version field exists, which is why an Environment leaf is undecidable.
    expect(environment).not.toHaveProperty('ObservedVersion');
  });
});