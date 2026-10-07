# Contributing to vATIS

Thank you for your interest in contributing to vATIS!

## How contributions work

All contributions are made as GitHub pull requests. The project owner reviews each one and will either merge it or ask for changes. All accepted contributions are licensed under [GPLv3](LICENSE).

## Before you start

- Search the [issues](https://github.com/vatis-project/vatis/issues) and [pull requests](https://github.com/vatis-project/vatis/pulls) to see whether someone has already raised the same idea or question.
- For larger changes, open an issue first to discuss the approach.
- Read the [local development documentation](https://vatis.app/docs/client/local-development) to learn how to build and run the project.
- Navdata changes belong in the [vATIS NavData](https://github.com/vatis-project/navdata) repository.

## Submitting a pull request

1. Create a [GitHub account](https://github.com/join) if you don't have one.
2. Click **Fork** at the top right of the [repository page](https://github.com/vatis-project/vatis).
3. Clone **your fork**:
   ```bash
   git clone https://github.com/<your-username>/vatis.git
   ```
4. Create a feature branch:
   ```bash
   git checkout -b my-feature-branch
   ```
5. Make your changes and commit them with a message that describes what changed. If the project has tests covering the area you changed, run `dotnet test` and add tests for new behavior where practical.
6. Push the branch to your fork:
   ```bash
   git push -u origin my-feature-branch
   ```
7. Open a pull request from your branch on GitHub. To update it, push more commits to the same branch.
8. Respond to review comments in the pull request. Once approved, it will be merged.
